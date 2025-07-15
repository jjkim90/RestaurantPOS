using DryIoc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Prism.DryIoc;
using Prism.Ioc;
using RestaurantPOS.Core.Interfaces;
using RestaurantPOS.Data.Context;
using RestaurantPOS.Data.Repositories;
using RestaurantPOS.Data.Seeders;
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
            return Container.Resolve<MainWindow>();
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

            // Services 등록 (나중에 추가)
            // containerRegistry.RegisterScoped<ITableService, TableService>();
            // containerRegistry.RegisterScoped<IMenuService, MenuService>();
            // containerRegistry.RegisterScoped<IOrderService, OrderService>();

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

                Log.Information("애플리케이션이 성공적으로 초기화되었습니다.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "애플리케이션 초기화 중 오류가 발생했습니다.");
                MessageBox.Show($"애플리케이션 시작 중 오류가 발생했습니다.\n{ex.Message}", 
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
