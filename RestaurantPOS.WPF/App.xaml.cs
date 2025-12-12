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
using RestaurantPOS.WPF.Modules.PaymentHistoryModule;
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
            try
            {
                System.Diagnostics.Debug.WriteLine("CreateShell 시작");
                
                // Set DevExpress Theme
                ApplicationThemeHelper.ApplicationThemeName = Theme.Office2019ColorfulName;
                System.Diagnostics.Debug.WriteLine("DevExpress 테마 설정 완료");
                
                var mainWindow = Container.Resolve<MainWindow>();
                System.Diagnostics.Debug.WriteLine($"MainWindow 생성 완료: {mainWindow != null}");
                
                mainWindow.Show();
                System.Diagnostics.Debug.WriteLine("MainWindow.Show() 호출 완료");
                
                return mainWindow;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "CreateShell에서 오류 발생");
                System.Windows.MessageBox.Show($"프로그램 시작 중 오류가 발생했습니다.\n\nCreateShell: {ex.Message}\n\n상세: {ex.InnerException?.Message}", 
                    "시작 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<TableModule>();
            moduleCatalog.AddModule<OrderModule>();
            moduleCatalog.AddModule<MenuModule>();
            moduleCatalog.AddModule<SettingsModule>();
            moduleCatalog.AddModule<PaymentHistoryModule>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            System.Diagnostics.Debug.WriteLine("RegisterTypes 시작");
            
            // Serilog 설정
            ConfigureLogging();

            try
            {
                System.Diagnostics.Debug.WriteLine("DbContext 등록 시작");
                
                // DbContext 등록 - 하드코딩된 연결 문자열 사용
                containerRegistry.Register<DbContextOptions<RestaurantContext>>(() =>
                {
                    var optionsBuilder = new DbContextOptionsBuilder<RestaurantContext>();
                    optionsBuilder.UseSqlServer(GetConnectionString());
                    return optionsBuilder.Options;
                });

                containerRegistry.RegisterScoped<RestaurantContext>();
                
                System.Diagnostics.Debug.WriteLine("DbContext 등록 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DbContext 등록 오류: {ex.Message}");
                throw;
            }

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
            containerRegistry.RegisterScoped<IPaymentHistoryService, PaymentHistoryService>();
            containerRegistry.RegisterSingleton<IPaymentSyncService, PaymentSyncService>();
            
            // Serilog ILogger 등록
            containerRegistry.RegisterInstance<ILogger>(Log.Logger);
            
            // Configuration 등록 - 1단계: Configuration만 먼저 등록
            try
            {
                System.Diagnostics.Debug.WriteLine("Configuration 등록 시작...");
                
                // Configuration 등록
                var configuration = BuildConfiguration();
                if (configuration != null)
                {
                    containerRegistry.RegisterInstance<IConfiguration>(configuration);
                    System.Diagnostics.Debug.WriteLine("Configuration DI 등록 완료");
                    Log.Information("Configuration successfully registered in DI container");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Configuration is null!");
                    Log.Warning("Configuration is null, skipping registration");
                }
                
                // 2단계: TossPaymentsService 등록
                System.Diagnostics.Debug.WriteLine("TossPaymentsService 등록 시작...");
                try
                {
                    containerRegistry.RegisterScoped<ITossPaymentsService, TossPaymentsService>();
                    System.Diagnostics.Debug.WriteLine("TossPaymentsService DI 등록 완료");
                    Log.Information("TossPaymentsService successfully registered in DI container");
                }
                catch (Exception serviceEx)
                {
                    System.Diagnostics.Debug.WriteLine($"TossPaymentsService 등록 오류: {serviceEx.Message}");
                    Log.Error(serviceEx, "Failed to register TossPaymentsService");
                    // 서비스 등록 실패해도 애플리케이션은 계속 실행
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Configuration 등록 오류: {ex.Message}");
                Log.Error(ex, "Failed to register Configuration in DI container");
                
                // 오류가 발생해도 애플리케이션은 계속 실행되도록 함
                // 빈 Configuration을 등록
                var emptyConfig = new ConfigurationBuilder().Build();
                containerRegistry.RegisterInstance<IConfiguration>(emptyConfig);
                System.Diagnostics.Debug.WriteLine("Empty Configuration registered as fallback");
            }
            
            // TossPaymentsService는 필요할 때 수동으로 등록

            // Views 등록
            containerRegistry.RegisterForNavigation<MainWindow>(nameof(MainWindow));
            
            System.Diagnostics.Debug.WriteLine("RegisterTypes 완료 - Configuration 및 TossPaymentsService 포함");
        }

        protected override async void OnInitialized()
        {
            base.OnInitialized();

            // WebView2 테스트를 위한 임시 코드
            #if DEBUG
            var testResult = System.Windows.MessageBox.Show(
                "WebView2 테스트 창을 열까요?\n\n" +
                "예: 테스트 창 열기\n" +
                "아니오: 정상적으로 POS 실행", 
                "WebView2 테스트", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
            
            if (testResult == MessageBoxResult.Yes)
            {
                var testWindow = new WebView2TestWindow();
                testWindow.ShowDialog();
                return;
            }
            #endif

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
                // 현재 실행 디렉토리 확인
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                System.Diagnostics.Debug.WriteLine($"Configuration BaseDirectory: {baseDirectory}");
                Log.Information("Configuration BaseDirectory: {BaseDirectory}", baseDirectory);
                
                // appsettings.json 파일 경로 확인
                var settingsPath = Path.Combine(baseDirectory, "appsettings.json");
                System.Diagnostics.Debug.WriteLine($"Looking for appsettings.json at: {settingsPath}");
                Log.Information("Looking for appsettings.json at: {SettingsPath}", settingsPath);
                
                // 파일 존재 여부 확인
                if (File.Exists(settingsPath))
                {
                    System.Diagnostics.Debug.WriteLine("appsettings.json found!");
                    Log.Information("appsettings.json file found successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("appsettings.json NOT found!");
                    Log.Warning("appsettings.json file not found at {SettingsPath}", settingsPath);
                }
                
                var builder = new ConfigurationBuilder()
                    .SetBasePath(baseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);  // WSL 환경 호환성을 위해 false로 설정

                System.Diagnostics.Debug.WriteLine("Building configuration...");
                var configuration = builder.Build();
                
                // 설정 내용 확인
                var tossPaymentsSection = configuration.GetSection("TossPayments");
                if (tossPaymentsSection.Exists())
                {
                    System.Diagnostics.Debug.WriteLine("TossPayments section found in configuration");
                    Log.Information("TossPayments configuration section loaded successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("TossPayments section NOT found in configuration");
                    Log.Warning("TossPayments configuration section not found");
                }
                
                System.Diagnostics.Debug.WriteLine("Configuration built successfully");
                Log.Information("Configuration built successfully");
                
                return configuration;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BuildConfiguration error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                Log.Error(ex, "Failed to build configuration");
                
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
                
            System.Diagnostics.Debug.WriteLine("Serilog 설정 완료");
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
