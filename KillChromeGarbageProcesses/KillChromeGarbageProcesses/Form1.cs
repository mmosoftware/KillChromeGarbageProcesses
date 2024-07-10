using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KillChromeGarbageProcesses
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ChromeProcessManager chromeProcessManager = new ChromeProcessManager();

            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--start-maximized");

            IWebDriver driver = new ChromeDriver(options);
            driver.Navigate().GoToUrl("https://www.google.com");

            // Thực hiện các hành động với Selenium WebDriver
            // ...

            // Kết thúc phiên làm việc
            driver.Quit();
        }
    }
}
