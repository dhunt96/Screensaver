using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32; // RegistryKey

namespace ScreenSaver
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void SaveSettings()
        {
            // Create or get existing Registry subkey
            RegistryKey key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Atoms_ScreenSaver");
            key.SetValue("atoms", numericUpDown1.Value.ToString());
        }

        private void LoadSettings()
        {
            // Get the value stored in the Registry
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Atoms_ScreenSaver");
            if (key == null)
                numericUpDown1.Value = 10;
            else
                numericUpDown1.Value = Convert.ToDecimal(key.GetValue("atoms"));
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            SaveSettings();
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

    }
}
