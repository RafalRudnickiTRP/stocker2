using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace WpfApplication3
{
    public partial class MainWindow : Window
    {
        private enum WorkMode
        {
            Selecting,
            Drawing
        }

        private WorkMode workMode;

        private void InitializeCommands()
        {
            workMode = WorkMode.Selecting;
        }

        private void CanExecute_SelectMode(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (workMode == WorkMode.Drawing);
        }
        
        private void CanExecute_DrawMode(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (workMode == WorkMode.Selecting); 
        }

        private void Command_SelectMode(object sender, ExecutedRoutedEventArgs e)
        {
            workMode = WorkMode.Selecting;
        }

        private void Command_DrawMode(object sender, ExecutedRoutedEventArgs e)
        {
            workMode = WorkMode.Drawing;
        }
    }
}
