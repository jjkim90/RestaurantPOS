using AutoMapper;
using DevExpress.Xpf.Core;
using DryIoc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using RestaurantPOS.Core.Interfaces;
using RestaurantPOS.Data.Context;
using RestaurantPOS.Data.Repositories;
using RestaurantPOS.Data.Seeders;
using RestaurantPOS.Services;
using RestaurantPOS.Services.Services;
using RestaurantPOS.Services.Mappings;
using RestaurantPOS.WPF.Infrastructure;
using RestaurantPOS.WPF.Modules.TableModule;
using RestaurantPOS.WPF.Modules.OrderModule;
using RestaurantPOS.WPF.Modules.MenuModule;
using RestaurantPOS.WPF.Modules.SettingsModule;
using RestaurantPOS.WPF.Views;
using Serilog;
using System;
using System.IO;
using System.Windows;

namespace RestaurantPOS.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            // Set DevExpress Theme
            ApplicationThemeHelper.ApplicationThemeName = Theme.Office2019ColorfulName;
            
            return Container.Resolve<MainWindow>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<TableModule>();
            moduleCatalog.AddModule<OrderModule>();
            moduleCatalog.AddModule<MenuModule>();
            moduleCatalog.AddModule<SettingsModule>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Serilog 설정
            ConfigureLogging();

            // DbContext 등록 - 하드코딩된 연결 문자열 사용
            containerRegistry.Register<DbContextOptions<RestaurantContext>>(() =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<RestaurantContext>();
                optionsBuilder.UseSqlServer(GetConnectionString());
                return optionsBuilder.Options;
            });

            containerRegistry.RegisterScoped<RestaurantContext>();

            // Repository 및 UnitOfWork 등록
            containerRegistry.RegisterScoped<IUnitOfWork, UnitOfWork>();
            containerRegistry.RegisterScoped(typeof(IRepository<>), typeof(GenericRepository<>));

            // Seeder 등록
            containerRegistry.RegisterScoped<DatabaseSeeder>();

            // AutoMapper 설정
            var config = new MapperConfiguration(cfg => {
                cfg.AddProfile<MappingProfile>();
                cfg.AddProfile<MenuMappingProfile>();
            }, new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory());
            var mapper = config.CreateMapper();
            containerRegistry.RegisterInstance<IMapper>(mapper);

            // Memory Cache 등록
            containerRegistry.RegisterSingleton<IMemoryCache>(() => new MemoryCache(new MemoryCacheOptions()));

            // ServiceScopeFactory 등록 (DryIoc 용)
            containerRegistry.RegisterSingleton<IServiceScopeFactory>(() => new DryIocServiceScopeFactory(Container.GetContainer()));

            // Services 등록
            containerRegistry.RegisterScoped<ITableService, TableService>();
            containerRegistry.RegisterSingleton<IMenuCacheService, MenuCacheService>();
            containerRegistry.RegisterScoped<IOrderService, OrderService>();
            containerRegistry.RegisterSingleton<IPrintService, PrintService>();
            containerRegistry.RegisterScoped<IMenuManagementService, MenuManagementService>();
            
            // Serilog ILogger 등록
            containerRegistry.RegisterInstance<ILogger>(Log.Logger);

            // Views 등록
            containerRegistry.RegisterForNavigation<MainWindow>(nameof(MainWindow));
        }

        protected override async void OnInitialized()
        {
            base.OnInitialized();

            try
            {
                // 데이터베이스 마이그레이션 및 시드 데이터 생성
                using (var scope = Container.CreateScope())
                {
                    var context = scope.Resolve<RestaurantContext>();
                    
                    // 데이터베이스가 없으면 생성
                    await context.Database.EnsureCreatedAsync();
                    
                    // 시드 데이터 생성
                    var seeder = scope.Resolve<DatabaseSeeder>();
                    await seeder.SeedAsync();
                }

                // Warm-up: EF Core 및 메뉴 데이터 캐시 사전 로딩
                using (var warmupScope = Container.CreateScope())
                {
                    var warmupContext = warmupScope.Resolve<RestaurantContext>();
                    // 간단한 쿼리로 EF Core 초기화
                    _ = await warmupContext.Categories.AnyAsync();
                    
                    // 메뉴 캐시 서비스로 카테고리와 첫 번째 카테고리의 메뉴 아이템 미리 로드
                    var menuCacheService = warmupScope.Resolve<IMenuCacheService>();
                    var categories = await menuCacheService.GetCategoriesAsync();
                    if (categories.Any())
                    {
                        // 첫 번째 카테고리의 메뉴 아이템도 미리 캐싱
                        await menuCacheService.GetMenuItemsByCategoryAsync(categories.First().CategoryId);
                    }
                }
                
                // DevExpress Grid 컨트롤 사전 로딩 (백그라운드에서)
                Task.Run(() =>
                {
                    try
                    {
                        // DevExpress Grid 관련 DLL 강제 로드
                        var gridType = typeof(DevExpress.Xpf.Grid.GridControl);
                        var imagesType = typeof(DevExpress.Images.ImageResourceCache);
                        System.Diagnostics.Debug.WriteLine($"DevExpress Grid preloaded: {gridType.Name}");
                    }
                    catch { }
                });

                // 초기 화면으로 TableManagementView 표시 (Prism Navigation 사용)
                var regionManager = Container.Resolve<Prism.Regions.IRegionManager>();
                regionManager.RequestNavigate("MainRegion", "TableManagementView");

                Log.Information("애플리케이션이 성공적으로 초기화되었습니다.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "애플리케이션 초기화 중 오류가 발생했습니다.");
                System.Windows.MessageBox.Show($"애플리케이션 시작 중 오류가 발생했습니다.\n{ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        private IConfiguration BuildConfiguration()
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                return builder.Build();
            }
            catch
            {
                // 오류 시 빈 Configuration 반환
                return new ConfigurationBuilder().Build();
            }
        }

        private void ConfigureLogging()
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "restaurantpos-.txt");
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }

        private string GetConnectionString()
        {
            // 나중에 필요하면 여기서 다른 방식으로 연결 문자열을 가져올 수 있음
            return "Server=.\\SQLEXPRESS;Database=RestaurantPOS;Integrated Security=true;TrustServerCertificate=true;";
        }
    }
}
