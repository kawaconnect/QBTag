using System;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Diagnostics;
using Logs;

namespace QuickBooksDAL;

public class DALUTIL
{
	[DebuggerNonUserCode]
	public DALUTIL()
	{
	}

	internal static OdbcConnection getQuickBooksConnection(string conStr)
	{
		return new OdbcConnection(conStr);
	}

	internal static OleDbConnection getAccessConnection(string conStr)
	{
		return new OleDbConnection(conStr);
	}

	/// <summary>
	/// Opens an Access DB connection with logging. Use this instead of raw con.Open()
	/// to get clear error messages when the DB can't be accessed (network drives, permissions, etc.)
	/// </summary>
	internal static void OpenAccessConnection(OleDbConnection con)
	{
		try
		{
			con.Open();
		}
		catch (Exception ex)
		{
			string path = "";
			try { path = con.DataSource; } catch { }
			Log.Add(ex, "Failed to open Access database: " + path);
			throw;
		}
	}

	/// <summary>
	/// Makes a value safe to place between single quotes in a SQL string
	/// (doubles any single quote). Use for values pasted into QODBC or Access queries.
	/// </summary>
	internal static string Quote(object value)
	{
		return Convert.ToString(value ?? "").Replace("'", "''");
	}

	internal static string gettest(string str)
	{
		str += " from Dal Util";
		return str;
	}
}
