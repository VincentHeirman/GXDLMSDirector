﻿//
// --------------------------------------------------------------------------
//  Gurux Ltd
// 
//
//
//
// Version:         $Revision: 9442 $,
//                  $Date: 2017-05-23 15:21:03 +0300 (ti, 23 touko 2017) $
//                  $Author: gurux01 $
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License 
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU General Public License for more details.
//
// More information of Gurux DLMS/COSEM Director: http://www.gurux.org/GXDLMSDirector
//
// This code is licensed under the GNU General Public License v2. 
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace GXDLMSDirector
{
    public partial class GXExternalMediaForm : Form
    {
        public string FileName
        {
            get;
            set;
        }

        public GXExternalMediaForm()
        {
            InitializeComponent();
        }

        private bool IsMedia(string fileName)
        {
            Assembly asm = Assembly.LoadFile(fileName);
            foreach (Type type in asm.GetTypes())
            {
                if (!type.IsAbstract && type.IsClass && typeof(IGXMedia).IsAssignableFrom(type))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check is file already added.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool IsAdded(string fileName)
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (a.GetName().FullName == fileName)
                {
                    return true;
                }
            }
            return false;
        }

        private void BrowseBtn_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Multiselect = false;
                dlg.InitialDirectory = Directory.GetCurrentDirectory();
                dlg.Filter = Properties.Resources.ExecutableFilesTxt;
                dlg.DefaultExt = ".dll";
                dlg.ValidateNames = true;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string file = dlg.FileName;
                    FileNameTb.Text = "file://" + file;
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Check is media downloaded.
        /// </summary>
        /// <param name="fileName">Media file name.</param>
        /// <returns>True, if downloaded.</returns>
        public static bool IsDownloaded(string fileName)
        {
            return fileName.StartsWith("http://") || fileName.StartsWith("https://") || fileName.StartsWith("www.");
        }

        /// <summary>
        /// Check if there are updates and download them to updates folder.
        /// </summary>
        /// <param name="updater"></param>
        /// <param name="asm"></param>
        /// <returns></returns>
        public static bool CheckUpdates(IGXUpdater updater, Assembly asm)
        {
            foreach (GXUpdateItem target in updater.CheckUpdates())
            {
                WebRequest req = WebRequest.Create(target.Source);
                using (HttpWebResponse response = req.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception(String.Format
                            ("Server error (HTTP {0}: {1}).",
                            response.StatusCode,
                            response.StatusDescription));
                    }
                    int length = 0;
                    var d = response.Headers["Content-Length"];
                    if (d != null)
                    {
                        length = int.Parse(d.ToString());
                    }
                    MemoryStream ms = new MemoryStream(length);
                    Stream stream = response.GetResponseStream();
                    byte[] buffer = new byte[length == 0 || length > 1024 ? 1024 : length];
                    IAsyncResult read = stream.BeginRead(buffer, 0, buffer.Length, null, null);
                    while (true)
                    {
                        // wait for the read operation to complete
                        read.AsyncWaitHandle.WaitOne();
                        int count = stream.EndRead(read);
                        ms.Write(buffer, 0, count);
                        // If read is done.
                        if (ms.Position == length || count == 0)
                        {
                            break;
                        }
                        read = stream.BeginRead(buffer, 0, buffer.Length, null, null);
                    }
                    ms.Position = 0;
                    using (FileStream w = File.Create(target.FileName, length))
                    {
                        w.Write(ms.GetBuffer(), 0, length);
                        w.Close();
                    }
                    AssemblyName current = asm.GetName();
                    AssemblyName updated = AssemblyName.GetAssemblyName(target.FileName);
                    // Compare both versions
                    if (updated.Version.CompareTo(current.Version) <= 0)
                    {
                        return false;
                    }
                    asm = System.Reflection.Assembly.GetExecutingAssembly();
                    string path = Path.Combine(Path.GetDirectoryName(asm.Location), "Updates");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    File.Copy(target.FileName, Path.Combine(path, target.FileName), true);
                }
            }
            return true;
        }

        public static bool DownLoadMedia(string name)
        {
            WebRequest req = WebRequest.Create(name);
            using (HttpWebResponse response = req.GetResponse() as HttpWebResponse)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(String.Format
                        ("Server error (HTTP {0}: {1}).",
                        response.StatusCode,
                        response.StatusDescription));
                }
                int length = 0;
                var d = response.Headers["Content-Length"];
                if (d != null)
                {
                    length = int.Parse(d.ToString());
                }
                MemoryStream ms = new MemoryStream(length);
                Stream stream = response.GetResponseStream();
                byte[] buffer = new byte[length == 0 || length > 1024 ? 1024 : length];
                IAsyncResult read = stream.BeginRead(buffer, 0, buffer.Length, null, null);
                while (true)
                {
                    // wait for the read operation to complete
                    read.AsyncWaitHandle.WaitOne();
                    int count = stream.EndRead(read);
                    ms.Write(buffer, 0, count);
                    // If read is done.
                    if (ms.Position == length || count == 0)
                    {
                        break;
                    }
                    read = stream.BeginRead(buffer, 0, buffer.Length, null, null);
                }
                ms.Position = 0;
                Assembly asm = Assembly.GetExecutingAssembly();
                string path = Path.Combine(Path.GetDirectoryName(asm.Location), "Medias");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path = Path.Combine(path, Path.GetFileName(name));
                try
                {
                    using (FileStream w = File.Create(path, length))
                    {
                        w.Write(ms.GetBuffer(), 0, length);
                        w.Close();
                    }
                    Assembly.LoadFile(path);
                }
                catch (Exception)
                {
                    //If file is in use.
                    path = Path.Combine(Path.GetDirectoryName(asm.Location), "Updates");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    path = Path.Combine(path, Path.GetFileName(name));
                    using (FileStream w = File.Create(path, length))
                    {
                        w.Write(ms.GetBuffer(), 0, length);
                        w.Close();
                    }
                    return true;
                }
            }
            return false;
        }

        private void OKBtn_Click(object sender, EventArgs e)
        {
            try
            {
                FileName = FileNameTb.Text;
                if (!IsDownloaded(FileName) && File.Exists(FileName))
                {
                    //Check that file is not added yet.
                    if (IsAdded(FileName))
                    {
                        throw new ArgumentException("File is already added.");
                    }
                    if (!IsMedia(FileName))
                    {
                        throw new ArgumentException("There are no media components to add.");
                    }
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }
    }
}