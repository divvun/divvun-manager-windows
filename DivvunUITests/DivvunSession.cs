using OpenQA.Selenium.Appium.Windows;
using System;
using System.Collections.Generic;
using System.Text;

namespace DivvunUITests
{
    public abstract class DivvunSession : IDisposable
    {
        protected readonly WindowsDriver<WindowsElement> Session;

        public DivvunSession()
        {
            var appiumOptions = new OpenQA.Selenium.Appium.AppiumOptions();
            appiumOptions.AddAdditionalCapability("app", @"C:\DivvunManager\DivvunInstaller.exe");
            this.Session = new WindowsDriver<WindowsElement>(new Uri("http://127.0.0.1:4723"), appiumOptions);
            this.Session.SwitchTo().Window(this.Session.WindowHandles[0]);

        }

        public void Dispose()
        {
            this.Session.Close();
            this.Session.Quit();
        }
    }
}
