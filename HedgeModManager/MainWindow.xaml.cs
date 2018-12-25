﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace HedgeModManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool IsCPKREDIRInstalled = false;
        public static ModsDB ModsDatabase;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void Refresh()
        {
            RefreshMods();
            RefreshUI();
        }

        public void RefreshMods()
        {
            ModsList.Items.Clear();
            ModsDatabase = new ModsDB(App.ModsDbPath);
            ModsDatabase.DetectMods();
            ModsDatabase.GetEnabledMods();
            ModsDatabase.Mods.ForEach(mod => ModsList.Items.Add(mod));

            // Re-arrange the mods
            for (int i = (int)ModsDatabase["Main"]["ActiveModCount", typeof(int)]; i >= 0; --i)
            {
                for (int i2 = 0; i2 < ModsList.Items.Count; i2++)
                {
                    var mod = ModsList.Items[i2] as ModInfo;
                    if (ModsDatabase["Main"][$"ActiveMod{i}"] == Path.GetFileName(mod.RootDirectory))
                    {
                        ModsList.Items.Remove(mod);
                        ModsList.Items.Insert(0, mod);
                    }
                }
            }
        }

        public void RefreshUI()
        {
            var steamGame = App.GetSteamGame(App.CurrentGame);
            IsCPKREDIRInstalled = App.IsCPKREDIRInstalled(App.GetSteamGame(App.CurrentGame).ExeDirectory);
            string loaders = (IsCPKREDIRInstalled ? "CPKREDIR v0.5" : "");
            bool hasOtherModLoader = File.Exists(Path.Combine(steamGame.RootDirectory, $"d3d{App.CurrentGame.DirectXVersion}.dll"));
            if (hasOtherModLoader)
            {
                if (string.IsNullOrEmpty(loaders))
                    loaders = $"{App.Config.ModLoaderName}";
                else
                    loaders += $" & {App.Config.ModLoaderName}";
            }

            Label_GameStatus.Content = $"Game Name: {App.CurrentGame.GameName}";
            Label_MLVersion.Content = $"Loaders: {loaders}";
            Button_OtherLoader.Content = hasOtherModLoader ? $"Uninstall {App.Config.ModLoaderName}" : $"Install {App.Config.ModLoaderName}";
            Button_CPKREDIR.Content = $"{(IsCPKREDIRInstalled ? "Uninstall" : "Install")} CPKREDIR";
        }

        public void SaveModsDB()
        {
            App.Config.Save(App.ConfigPath);
            ModsDatabase.Mods.Clear();
            foreach (var mod in ModsList.Items)
            {
                ModsDatabase.Mods.Add(mod as ModInfo);
            }
            ModsDatabase.SaveDB();
        }

        public void StartGame()
        {
            App.GetSteamGame(App.CurrentGame).StartGame();

            if(!App.Config.KeepOpen)
                Application.Current.Shutdown(0);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //var timer = new DispatcherTimer();
            //timer.Tick += dispatcherTimer_Tick;
            //timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            //timer.Start();
            
            // App.Config
            DataContext = App.Config;

            Refresh();

        }


        private void UI_MoveMod_Click(object sender, RoutedEventArgs e)
        {
            var index = Math.Max(0, ModsList.SelectedIndex);
            var mod = ModsList.Items[index];
            if (sender.Equals(UpBtn))
            {
                ModsList.Items.RemoveAt(index);
                ModsList.Items.Insert(Math.Max(0, --index), mod);
            }
            else if (sender.Equals(TopBtn))
            {
                ModsList.Items.RemoveAt(index);
                ModsList.Items.Insert(0, mod);
            }
            else if (sender.Equals(DownBtn))
            {
                ModsList.Items.RemoveAt(index);
                ModsList.Items.Insert(++index, mod);
            }
            else if (sender.Equals(BottomBtn))
            {
                ModsList.Items.RemoveAt(index);
                ModsList.Items.Insert(ModsList.Items.Count, mod);
            }
            ModsList.SelectedIndex = index;
        }

        // TODO: RemoveMod

        private void UI_RemoveMod_Click(object sender, RoutedEventArgs e)
        {
            var mod = ModsList.SelectedValue as ModInfo;
            if (mod == null)
                return;

            var box = new HedgeMessageBox("WARNING", string.Format(Properties.Resources.STR_UI_DELETEMOD, mod.Title));
            
            box.AddButton("  Cancel  ", () =>
            {
                box.Close();
            });
            box.AddButton("Delete", () =>
            {
                ModsDatabase.DeleteMod(ModsList.SelectedItem as ModInfo);
                Refresh();
                box.Close();
            });
            box.ShowDialog();
            Refresh();
        }

        private void UI_Refresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        // TODO: AddMod

        private void UI_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveModsDB();
            Refresh();
        }

        private void UI_SaveAndPlay_Click(object sender, RoutedEventArgs e)
        {
            SaveModsDB();
            Refresh();
            StartGame();
        }

        private void UI_Play_Click(object sender, RoutedEventArgs e)
        {
            StartGame();
        }

        private void UI_CPKREDIR_Click(object sender, RoutedEventArgs e)
        {
            App.InstallCPKREDIR(App.GetSteamGame(App.CurrentGame).ExeDirectory, IsCPKREDIRInstalled);
            RefreshUI();
        }

        private void UI_About_Click(object sender, RoutedEventArgs e)
        {
            new AboutWindow().ShowDialog();
        }

        private void UI_ModsList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                // Try Install mods from all files
                files.ToList().ForEach(t => ModsDatabase.InstallMod(t));
                Refresh();
            }
        }

        private void UI_OtherLoader_Click(object sender, RoutedEventArgs e)
        {
            App.InstallOtherLoader(true);
            RefreshUI();
        }

        // too slow
        // :^)
        // LOL
        //private void dispatcherTimer_Tick(object sender, EventArgs e)
        //{
        //    (RotateTest.RenderTransform as RotateTransform).Angle += 10d;
        //}
    }
}