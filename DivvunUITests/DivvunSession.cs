using OpenQA.Selenium.Appium.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace DivvunUITests
{
    public abstract class DivvunSession : IDisposable
    {
        protected readonly WindowsDriver<WindowsElement> Session;

        public DivvunSession()
        {
            try
            {
            var appiumOptions = new OpenQA.Selenium.Appium.AppiumOptions();
            appiumOptions.AddAdditionalCapability("app", @"C:\DivvunManager\DivvunManager.exe");
            this.Session = new WindowsDriver<WindowsElement>(new Uri("http://127.0.0.1:4723"), appiumOptions, TimeSpan.FromSeconds(20));
            this.Session.SwitchTo().Window(this.Session.WindowHandles[0]);
            }
            catch
            {
                this.SaveScreenshot();
                throw;
            }

        }

        public void Dispose()
        {
            this.Session.Close();
            this.Session.Quit();
        }

        protected void SaveScreenshot()
        {
            var basePath = @"C:\DivvunManager\Screenshots";
            Console.WriteLine("Saving Screenshot");

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            var fileName = $"image_{Directory.GetFiles(basePath).Length + 1}.jpg";

            Console.WriteLine(string.Join(";", Directory.GetFiles(basePath)));

            using var bitmap = new Bitmap(1920, 1080);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(0, 0, 0, 0,
                bitmap.Size, CopyPixelOperation.SourceCopy);
            }
            bitmap.Save($@"{basePath}\{fileName}", ImageFormat.Jpeg);
        }
    }
}
