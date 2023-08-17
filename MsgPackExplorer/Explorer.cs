﻿using LsMsgPack;
using System;
using System.Windows.Forms;
using System.Linq;
using System.IO;

namespace MsgPackExplorer {
  public partial class Explorer: Form {
    public Explorer() {
      InitializeComponent();
      ddLimitItems.SelectedIndex = 0;

      ddEndianess.Items.AddRange(new[]{
        new EndianChoice(EndianAction.SwapIfCurrentSystemIsLittleEndian, "Reorder if system is little endian (default)."),
        new EndianChoice(EndianAction.NeverSwap, "Never reorder"),
        new EndianChoice(EndianAction.AlwaysSwap, "Always reorder")
      });
      ddEndianess.SelectedIndex = 0;
    }

    private void btnOpen_Click(object sender, EventArgs e) {
      if(openFileDialog1.ShowDialog() == DialogResult.OK) {
                openFile(openFileDialog1.FileName);
      }
    }

    private void btnGenerateTestFiles_Click(object sender, EventArgs e) {
      if(saveTestSuiteDialog.ShowDialog()== DialogResult.OK) {
        new TestFileSuiteCreator().CreateSuite(System.IO.Path.GetDirectoryName(saveTestSuiteDialog.FileName));
      }
    }

    private void aboutToolStripMenuItem_Click(object sender, EventArgs e) {
      new AboutBox().ShowDialog(this);
    }

    private void btnProcessAfterError_CheckedChanged(object sender, EventArgs e) {
      msgPackExplorer1.ContinueOnError = btnProcessAfterError.Checked;
    }

    private void ddLimitItems_TextChanged(object sender, EventArgs e) {
      long limit;
      if (long.TryParse(ddLimitItems.Text, out limit))
        msgPackExplorer1.DisplayLimit = limit;
      else
        msgPackExplorer1.DisplayLimit = long.MaxValue;
      msgPackExplorer1.RefreshTree();
    }

    private void ddEndianess_DropDownClosed(object sender, EventArgs e) {
      EndianChoice choice = ddEndianess.SelectedItem as EndianChoice;
      if (choice is null)
        return;
      msgPackExplorer1.EndianHandling = choice.Value;
      msgPackExplorer1.Data = msgPackExplorer1.Data;
    }

    private void installAsFiddlerInspectorToolStripMenuItem_Click(object sender, EventArgs e) {
      try {
        if (!Installer.TryInstall()) {
          MessageBox.Show("Unable to find the Fiddler application files", "Not installed", MessageBoxButtons.OK, MessageBoxIcon.Error);
          return;
        }
        
        if(Installer.FiddlerIsRunning)
          MessageBox.Show("Installed successfully.\r\nFiddler is currently running.\r\nYou will need to restart Fiddler in order to use the MsgPack inspector.", "Installed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        else
          MessageBox.Show("Installed successfully.", "Installed", MessageBoxButtons.OK, MessageBoxIcon.Information);

      } catch(Exception ex) {
        MessageBox.Show(string.Concat("Inastallation failed with the following message:\r\n", ex.Message,"\r\n\r\nYou may have more luck (depending on the error) running with administration privileges.") , "Not installed", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
        }
        private void Explorer_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                   e.Effect= DragDropEffects.None;
            }
        }

        private void Explorer_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                openFile(files[0]);
            }
        }

        private void openFile(string path)
        {
            if (! File.Exists(path) || File.GetAttributes(path).HasFlag(FileAttributes.Directory)) {
                return;
            }
            msgPackExplorer1.Data = System.IO.File.ReadAllBytes(path);
            Text = openFileDialog1.FileName + " - MsgPack Explorer";
        }

    }

    public class EndianChoice {
    public EndianChoice(EndianAction value, string description) {
      Value = value;
      Description = description;
    }

    private string Description { get; }
    public EndianAction Value { get; }

    public override string ToString() {
      return Description;
    }
  }
}
