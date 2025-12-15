using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using System;
using System.IO;

namespace SmartDesktop.Tests.UI
{
    public abstract class AutomationBase : IDisposable
    {
        protected Application App { get; private set; }
        protected UIA3Automation Automation { get; private set; }
        protected Window MainWindow { get; private set; }

        public AutomationBase()
        {
            var appPath = Path.GetFullPath(@"..\..\..\..\src\SmartDesktop.UI\bin\Debug\net8.0-windows\SmartDesktop.UI.exe");
            
            // Check if app exists before trying to launch (for test robustness during dev)
            if (!File.Exists(appPath))
            {
               // In a real scenario, we might build it here or throw a clearer error
               // throw new FileNotFoundException("Application not found. Please build the solution.", appPath);
            }

            // Launch the application
            App = Application.Launch(appPath);
            Automation = new UIA3Automation();
            MainWindow = App.GetMainWindow(Automation);
        }

        public void Dispose()
        {
            Automation?.Dispose();
            App?.Close();
        }
    }
}
