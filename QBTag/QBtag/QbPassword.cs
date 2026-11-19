using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using QBtag.My;

namespace QBtag;

/// <summary>
/// The QuickBooks (QODBC) password. Never kept in source or in the plain config file.
/// Where it comes from, first match wins:
///   1. the QB_PASSWORD environment variable (servers, scripted or portable runs)
///   2. %ProgramData%\QBTag\qb-password.dat, encrypted with Windows DPAPI for this computer
///      (written by the installer, by "QBTag --set-qb-password", or by the prompt in the app)
///   3. the legacy QuickBooksPassword setting
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
				Logs.Log.Add("QuickBooks password file could not be read (" + ex.Message + "). Set it again with QBTag --set-qb-password.");
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

	/// <summary>True when the connection string expects a password and none has been set up.</summary>
	public static bool Missing(string connectionString)
	{
		return connectionString != null && connectionString.Contains(Placeholder) && Get() == null;
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

	/// <summary>True when an ODBC error looks like a rejected or missing login.</summary>
	public static bool LooksLikeLoginError(Exception ex)
	{
		string m = (ex?.ToString() ?? "").ToLowerInvariant();
		return m.Contains("password") || m.Contains("login") || m.Contains("logon") || m.Contains("authenticat");
	}

	/// <summary>
	/// Ask for the password in a small dialog and store it. Returns true when something was saved.
	/// </summary>
	public static bool Prompt(IWin32Window owner, string reason)
	{
		using Form f = new Form
		{
			Text = "QuickBooks login",
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
			Height = 64,
			Text = reason + "\r\n\r\nQBTag uses this QuickBooks login for the QODBC connection. The password is stored encrypted on this computer."
		};
		Label userLbl = new Label { Left = 12, Top = 84, Width = 120, Text = "QuickBooks user:" };
		TextBox user = new TextBox
		{
			Left = 132,
			Top = 81,
			Width = 276,
			Text = EffectiveUser(MySettingsProperty.Settings.QuickBooksDBConnectionString)
		};
		Label pwLbl = new Label { Left = 12, Top = 116, Width = 120, Text = "Password:" };
		TextBox pw = new TextBox
		{
			Left = 132,
			Top = 113,
			Width = 276,
			UseSystemPasswordChar = true
		};
		CheckBox none = new CheckBox
		{
			Left = 132,
			Top = 144,
			Width = 276,
			Text = "This user has no password"
		};
		none.CheckedChanged += (s, e) => pw.Enabled = !none.Checked;
		Button ok = new Button
		{
			Text = "Save",
			Left = 252,
			Top = 210,
			Width = 75,
			DialogResult = DialogResult.OK
		};
		Button cancel = new Button
		{
			Text = "Later",
			Left = 333,
			Top = 210,
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
			MessageBox.Show(owner, "No password entered, nothing was saved.", "QuickBooks password", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return false;
		}
		if (user.Text.Trim().Length == 0)
		{
			MessageBox.Show(owner, "Enter the QuickBooks user name (Admin by default).", "QuickBooks login", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
			MessageBox.Show(owner, "The password could not be saved: " + ex.Message + "\r\n\r\nYou can also set the QB_PASSWORD environment variable, or run QBTag --set-qb-password as an administrator.", "QuickBooks password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return false;
		}
	}

	/// <summary>Called when the app starts: if the password is needed and not set up, ask for it.</summary>
	public static void EnsureConfigured(IWin32Window owner)
	{
		if (Missing(MySettingsProperty.Settings.QuickBooksDBConnectionString))
		{
			Prompt(owner, "QBTag needs the QuickBooks password to read sales orders, and none is set up on this computer.");
		}
	}

	private static bool _offered;

	/// <summary>
	/// Run a QuickBooks query. If QuickBooks rejects the login, explain it and offer to enter the
	/// password (once per session), call onSaved when a new password was stored, then rethrow.
	/// </summary>
	public static T Guard<T>(IWin32Window owner, Func<T> query, Action onSaved)
	{
		try
		{
			return query();
		}
		catch (Exception ex) when (LooksLikeLoginError(ex))
		{
			Logs.Log.Add(ex);
			if (OfferAfterLoginError(owner, ex))
			{
				onSaved?.Invoke();
			}
			throw;
		}
	}

	/// <summary>Called after a failed QuickBooks query: explain and offer to (re)enter the password.</summary>
	public static bool OfferAfterLoginError(IWin32Window owner, Exception ex)
	{
		if (!LooksLikeLoginError(ex) || _offered)
		{
			return false;
		}
		_offered = true;
		string reason = Get() == null
			? "QuickBooks refused the connection and no QuickBooks password is set up on this computer."
			: "QuickBooks refused the connection. The stored QuickBooks password may be wrong or out of date.";
		return Prompt(owner, reason + " After saving, run the action again.");
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
