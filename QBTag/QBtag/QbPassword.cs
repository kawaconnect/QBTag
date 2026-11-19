using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using QBtag.My;

namespace QBtag;

/// <summary>
/// The optional QuickBooks login for QODBC connections. The order screens read QuickBooks through
/// the QuickBooks SDK (QBFC), which ignores it; only the ODBC code paths use Resolve().
/// Never kept in source or in the plain config file. Where the password comes from, first match wins:
///   1. the QB_PASSWORD environment variable (servers, scripted or portable runs)
///   2. %ProgramData%\QBTag\qb-password.dat, encrypted with Windows DPAPI for this computer
///      (written from Tools > Configure Databases)
///   3. the legacy QuickBooksPassword setting (empty by default)
/// The QuickBooks connection string carries the placeholder ${QB_PASSWORD}; Resolve() fills it in.
/// </summary>
internal static class QbPassword
{
	public const string Placeholder = "${QB_PASSWORD}";

	private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("QBTag.QuickBooksPassword.v1");

	/// <summary>Plain-text file holding the QuickBooks user name (not a secret).</summary>
	public static string UserPath
	{
		get
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "QBTag", "qb-user.txt");
		}
	}

	/// <summary>
	/// The QuickBooks user QBTag logs in as: QB_USER environment variable, then the stored user,
	/// then null (keep whatever uid= the connection string has, "Admin" by default).
	/// </summary>
	public static string GetUser()
	{
		string env = Environment.GetEnvironmentVariable("QB_USER");
		if (!string.IsNullOrEmpty(env))
		{
			return env;
		}
		try
		{
			if (File.Exists(UserPath))
			{
				string u = File.ReadAllText(UserPath).Trim();
				return u.Length == 0 ? null : u;
			}
		}
		catch (Exception ex)
		{
			Logs.Log.Add("QuickBooks user file could not be read: " + ex.Message);
		}
		return null;
	}

	public static void SaveUser(string user)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(UserPath));
		File.WriteAllText(UserPath, (user ?? "").Trim());
	}

	/// <summary>The user name the connection will use (for display and as the dialog default).</summary>
	public static string EffectiveUser(string connectionString)
	{
		string u = GetUser();
		if (u != null)
		{
			return u;
		}
		System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(connectionString ?? "", @"(?i)(?:^|;)\s*uid\s*=\s*([^;]*)");
		return m.Success ? m.Groups[1].Value : "Admin";
	}

	public static string StorePath
	{
		get
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "QBTag", "qb-password.dat");
		}
	}

	/// <summary>Where the password currently comes from: "environment", "stored", "setting" or "none".</summary>
	public static string Source
	{
		get
		{
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("QB_PASSWORD")))
			{
				return "environment";
			}
			if (File.Exists(StorePath))
			{
				return "stored";
			}
			if (!string.IsNullOrEmpty(SafeSetting()))
			{
				return "setting";
			}
			return "none";
		}
	}

	/// <summary>The password, or null when none is configured. An empty string means "no password" was chosen.</summary>
	public static string Get()
	{
		string env = Environment.GetEnvironmentVariable("QB_PASSWORD");
		if (!string.IsNullOrEmpty(env))
		{
			return env;
		}
		if (File.Exists(StorePath))
		{
			try
			{
				byte[] data = ProtectedData.Unprotect(File.ReadAllBytes(StorePath), Entropy, DataProtectionScope.LocalMachine);
				return Encoding.UTF8.GetString(data);
			}
			catch (Exception ex)
			{
				Logs.Log.Add("QuickBooks password file could not be read (" + ex.Message + "). Set it again under Tools > Configure Databases.");
			}
		}
		string setting = SafeSetting();
		return string.IsNullOrEmpty(setting) ? null : setting;
	}

	/// <summary>Store the password for every user of this computer. An empty string records "no password".</summary>
	public static void Save(string password)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(StorePath));
		byte[] data = ProtectedData.Protect(Encoding.UTF8.GetBytes(password ?? ""), Entropy, DataProtectionScope.LocalMachine);
		File.WriteAllBytes(StorePath, data);
	}

	public static bool Clear()
	{
		if (!File.Exists(StorePath))
		{
			return false;
		}
		File.Delete(StorePath);
		return true;
	}

	/// <summary>Remove the stored user and password. Returns true when anything was removed.</summary>
	public static bool ClearAll()
	{
		bool removed = Clear();
		if (File.Exists(UserPath))
		{
			File.Delete(UserPath);
			removed = true;
		}
		return removed;
	}

	/// <summary>One line for the settings window: what is set, without the password itself.</summary>
	public static string Describe()
	{
		string user = GetUser();
		string source = Source;
		if (user == null && source == "none")
		{
			return "Not set.";
		}
		string pw;
		switch (source)
		{
			case "environment": pw = "password from the QB_PASSWORD environment variable"; break;
			case "stored": pw = Get() == "" ? "no password" : "password stored encrypted on this computer"; break;
			case "setting": pw = "password from the legacy QuickBooksPassword setting"; break;
			default: pw = "no password set"; break;
		}
		return "Set: user " + (user ?? "(from the connection string)") + ", " + pw + ".";
	}

	/// <summary>The connection string with the placeholder replaced by the password (empty when none is set).</summary>
	public static string Resolve(string connectionString)
	{
		if (connectionString == null)
		{
			return null;
		}
		string user = GetUser();
		if (user != null)
		{
			if (System.Text.RegularExpressions.Regex.IsMatch(connectionString, @"(?i)(^|;)\s*uid\s*="))
			{
				connectionString = System.Text.RegularExpressions.Regex.Replace(connectionString, @"(?i)((?:^|;)\s*uid\s*=)[^;]*", m => m.Groups[1].Value + user.Replace(";", ""));
			}
		}
		if (!connectionString.Contains(Placeholder))
		{
			return connectionString;
		}
		return connectionString.Replace(Placeholder, Get() ?? "");
	}

	/// <summary>
	/// Ask for the QODBC user and password in a small dialog and store them. Returns true when something was saved.
	/// </summary>
	public static bool Prompt(IWin32Window owner, string reason)
	{
		using Form f = new Form
		{
			Text = "QuickBooks ODBC login (optional)",
			FormBorderStyle = FormBorderStyle.FixedDialog,
			StartPosition = FormStartPosition.CenterScreen,
			MinimizeBox = false,
			MaximizeBox = false,
			ShowInTaskbar = owner == null,
			ClientSize = new Size(420, 250)
		};
		Label info = new Label
		{
			Left = 12,
			Top = 12,
			Width = 396,
			Height = 96,
			Text = reason + "\r\n\r\nOnly used for QODBC connections. The order screens read QuickBooks through the QuickBooks SDK and do not use it. The password is stored encrypted on this computer."
		};
		Label userLbl = new Label { Left = 12, Top = 116, Width = 120, Text = "QuickBooks user:" };
		TextBox user = new TextBox
		{
			Left = 132,
			Top = 113,
			Width = 276,
			Text = EffectiveUser(MySettingsProperty.Settings.QuickBooksDBConnectionString)
		};
		Label pwLbl = new Label { Left = 12, Top = 148, Width = 120, Text = "Password:" };
		TextBox pw = new TextBox
		{
			Left = 132,
			Top = 145,
			Width = 276,
			UseSystemPasswordChar = true
		};
		CheckBox none = new CheckBox
		{
			Left = 132,
			Top = 176,
			Width = 276,
			Text = "This user has no password"
		};
		none.CheckedChanged += (s, e) => pw.Enabled = !none.Checked;
		Button ok = new Button
		{
			Text = "Save",
			Left = 252,
			Top = 214,
			Width = 75,
			DialogResult = DialogResult.OK
		};
		Button cancel = new Button
		{
			Text = "Cancel",
			Left = 333,
			Top = 214,
			Width = 75,
			DialogResult = DialogResult.Cancel
		};
		f.Controls.AddRange(new Control[] { info, userLbl, user, pwLbl, pw, none, ok, cancel });
		f.AcceptButton = ok;
		f.CancelButton = cancel;
		if (f.ShowDialog(owner) != DialogResult.OK)
		{
			return false;
		}
		if (!none.Checked && pw.Text.Length == 0)
		{
			MessageBox.Show(owner, "No password entered, nothing was saved. Tick \"This user has no password\" if that is intended.", "QuickBooks ODBC login", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return false;
		}
		if (user.Text.Trim().Length == 0)
		{
			MessageBox.Show(owner, "Enter the QuickBooks user name (Admin by default).", "QuickBooks ODBC login", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return false;
		}
		try
		{
			SaveUser(user.Text);
			Save(none.Checked ? "" : pw.Text);
			return true;
		}
		catch (Exception ex)
		{
			MessageBox.Show(owner, "The login could not be saved: " + ex.Message + "\r\n\r\nThe current Windows user cannot write to " + Path.GetDirectoryName(StorePath) + ". Save it once as an administrator, or set the QB_USER and QB_PASSWORD environment variables instead.", "QuickBooks ODBC login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return false;
		}
	}

	private static string SafeSetting()
	{
		try
		{
			return MySettingsProperty.Settings.QuickBooksPassword;
		}
		catch
		{
			return null;
		}
	}
}
