using System;
using System.IO;
using System.Windows;
using CrystalLauncher.Models;
using CrystalLauncher.Services;
using CrystalLauncher.ViewModels;

namespace CrystalLauncher;

public partial class App : Application
{
	protected override async void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		var baseDirectory = AppContext.BaseDirectory;
		var configDirectory = Path.Combine(baseDirectory, "Config");
		var configPath = Path.Combine(configDirectory, "launcher.json");
		var templatePath = Path.Combine(configDirectory, "launcher.template.json");

		var configService = new LauncherConfigService(configPath, templatePath);

		LauncherConfig config;
		try
		{
			config = await configService.LoadAsync();
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Falha ao carregar configurações do launcher: {ex.Message}", "Crystal Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
			Shutdown();
			return;
		}

		var viewModel = new LauncherViewModel(config, configService);
		var mainWindow = new MainWindow
		{
			DataContext = viewModel
		};

		MainWindow = mainWindow;
		mainWindow.Show();
	}
}

