using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;

namespace QBtag.My;

[EditorBrowsable(EditorBrowsableState.Never)]
[GeneratedCode("MyTemplate", "10.0.0.0")]
internal class MyApplication : WindowsFormsApplicationBase
{
	private static List<WeakReference> __ENCList = new List<WeakReference>();

	[DebuggerNonUserCode]
	private static void __ENCAddToList(object value)
	{
		checked
		{
			lock (__ENCList)
			{
				if (__ENCList.Count == __ENCList.Capacity)
				{
					int num = 0;
					int num2 = __ENCList.Count - 1;
					int num3 = 0;
					while (true)
					{
						int num4 = num3;
						int num5 = num2;
						if (num4 > num5)
						{
							break;
						}
						WeakReference weakReference = __ENCList[num3];
						if (weakReference.IsAlive)
						{
							if (num3 != num)
							{
								__ENCList[num] = __ENCList[num3];
							}
							num++;
						}
						num3++;
					}
					__ENCList.RemoveRange(num, __ENCList.Count - num);
					__ENCList.Capacity = __ENCList.Count;
				}
				__ENCList.Add(new WeakReference(RuntimeHelpers.GetObjectValue(value)));
			}
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
	[STAThread]
	[DebuggerHidden]
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static void Main(string[] Args)
	{
		UpgradeSettingsIfNeeded();
		if (QBtag.CLI.HasArgs(Args))
		{
			int exitCode = QBtag.CLI.Run(Args);
			Environment.Exit(exitCode);
			return;
		}
		try
		{
			Application.SetCompatibleTextRenderingDefault(WindowsFormsApplicationBase.UseCompatibleTextRendering);
		}
		finally
		{
		}
		MyProject.Application.Run(Args);
	}

	/// <summary>
	/// Per-user settings (database path, reports) live in a folder named after the version, so
	/// each new version would start from the defaults. On the first start of a new version, copy
	/// the previous version's values over (both the window and the command line come through here).
	/// </summary>
	private static void UpgradeSettingsIfNeeded()
	{
		try
		{
			MySettings settings = MySettingsProperty.Settings;
			if (settings.UpgradeRequired)
			{
				settings.Upgrade();
				// "Usman" was the shipped default of the old QuickBooksPassword setting, not a real login.
				if (settings.QuickBooksPassword == "Usman")
				{
					settings.QuickBooksPassword = "";
				}
				settings.UpgradeRequired = false;
				settings.Save();
			}
		}
		catch (Exception ex)
		{
			// A damaged user.config must not stop QBTag from starting; it runs with the defaults.
			try
			{
				Logs.Log.Add("Previous settings could not be carried over: " + ex.Message);
			}
			catch
			{
			}
		}
	}

	[DebuggerStepThrough]
	public MyApplication()
		: base(AuthenticationMode.Windows)
	{
		__ENCAddToList(this);
		IsSingleInstance = false;
		EnableVisualStyles = true;
		SaveMySettingsOnExit = false;
		ShutdownStyle = ShutdownMode.AfterMainFormCloses;
	}

	[DebuggerStepThrough]
	protected override void OnCreateMainForm()
	{
		MainForm = MyProject.Forms.FrmMain;
	}
}
