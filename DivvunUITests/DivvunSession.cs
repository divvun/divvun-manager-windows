using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace DivvunUITests {

public abstract class DivvunSession : IDisposable {
    protected readonly WindowsDriver<WindowsElement> Session;

    public DivvunSession() {
        try {
            var appiumOptions = new AppiumOptions();
            appiumOptions.AddAdditionalCapability("app", @"C:\DivvunManager\DivvunManager.exe");
            Session = new WindowsDriver<WindowsElement>(new Uri("http://127.0.0.1:4723"), appiumOptions,
                TimeSpan.FromSeconds(20));
            Session.SwitchTo().Window(Session.WindowHandles[0]);
        }
        catch {
            SaveScreenshot();
            throw;
        }
    }

    public void Dispose() {
        Session.Close();
        Session.Quit();
    }

    protected void SaveScreenshot() {
        var basePath = @"C:\DivvunManager\Screenshots";
        Console.WriteLine("Saving Screenshot");

        if (!Directory.Exists(basePath)) {
            Directory.CreateDirectory(basePath);
        }

        var fileName = $"image_{Directory.GetFiles(basePath).Length + 1}.jpg";

        Console.WriteLine(string.Join(";", Directory.GetFiles(basePath)));

        using var bitmap = new Bitmap(1920, 1080);
        using (var g = Graphics.FromImage(bitmap)) {
            g.CopyFromScreen(0, 0, 0, 0,
                bitmap.Size, CopyPixelOperation.SourceCopy);
        }

        bitmap.Save($@"{basePath}\{fileName}", ImageFormat.Jpeg);
    }
}

}