﻿#region License
/*
	updateSystem.NET - Easy to use Autoupdatesolution for .NET Apps
	Copyright (C) 2012  Maximilian Krauss <max@kraussz.com>
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
#endregion

using System;
using System.ComponentModel;
using System.IO;
using updateSystemDotNet.Setup.Core;

namespace updateSystemDotNet.Setup.UI.setupPages {
	internal partial class stpUninstall : basePage, ISetupPage {
		public stpUninstall() {
			InitializeComponent();
			bgwUninstall.ProgressChanged += bgwUninstall_ProgressChanged;
			bgwUninstall.RunWorkerCompleted += bgwUninstall_RunWorkerCompleted;
		}

		#region ISetupPage Members

		public setupContext Context { get; set; }

		public string Title {
			get { return "Die Deinstallation wird durchgeführt"; }
		}

		public bool isLastPage {
			get { return false; }
		}

		public basePage View {
			get { return this; }
		}

		public Type forwardPage {
			get { return null; }
		}

		public Type backwardPage {
			get { return null; }
		}

		#endregion

		public override void onShow() {
			base.onShow();
			bgwUninstall.RunWorkerAsync(Context);
		}

		private void bgwUninstall_DoWork(object sender, DoWorkEventArgs e) {
			var context = (e.Argument as setupContext);
			try {
				string baseDir = AppDomain.CurrentDomain.BaseDirectory;

				//Zu entfernende Dateien ermitteln
				string[] files = Directory.GetFiles(baseDir, "*", SearchOption.AllDirectories);
				int currentFile = 0;
				//Versuchen Dateien zu löschen
				foreach (string file in files) {
					try {
						//Versuchen das native Abbild der Datei zu entfernen
						if (file.EndsWith(".exe") || file.EndsWith(".dll"))
							nativeImages.Uninstall(file);

						File.Delete(file);
						currentFile++;
						bgwUninstall.ReportProgress(Percent(currentFile, files.Length));
					}
					catch {
					}
				}

				//Programm aus der Systemregistrierung entfernen
				ProgramsAndFeaturesHelper.Remove(context.Product);

				//Settings löschen
				if (context.removeSettings) {
					string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
					                                   "updateSystem.NET");
					if (Directory.Exists(settingsPath)) {
						Directory.Delete(settingsPath, true);
					}
				}

				//Verknüpfungen entfernen
				string scDesktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
				                                "updateSystem.NET Administration.lnk");
				string scStartMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),
				                                  "updateSystem.NET Administration.lnk");

				if (File.Exists(scDesktop))
					File.Delete(scDesktop);

				if (File.Exists(scStartMenu))
					File.Delete(scStartMenu);

				//Dateitypeverknüpfungen entfernen (nur in Releaseversion)
				if (context.Product.GetType() == typeof (productRTM)) {
					if (FileAssociation.IsAssociated(".udprojx"))
						FileAssociation.DeleteAssociation(".udprojx");

					if (FileAssociation.IsAssociated(".udproj"))
						FileAssociation.DeleteAssociation(".udproj");

					FileAssociation.refreshDesktop();
				}
			}
			catch (Exception exc) {
				e.Result = exc;
			}
		}

		private int Percent(long CurrVal, long MaxVal) {
			try {
				return (int) (((CurrVal/((double) MaxVal))*100.0));
			}
			catch {
				return 100;
			}
		}

		private void bgwUninstall_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
			if (e.Result == null) {
				onChangePage(new changePageEventArgs(typeof (stpUninstalled)));
			}
			else {
				Context.setupException = (e.Result as Exception);
				onChangePage(new changePageEventArgs(typeof (stpInterrupted)));
			}
		}

		private void bgwUninstall_ProgressChanged(object sender, ProgressChangedEventArgs e) {
			prgCopyFiles.Value = e.ProgressPercentage;
		}
	}
}