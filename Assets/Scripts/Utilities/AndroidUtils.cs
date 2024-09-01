using System.IO;
using UnityEngine;

public static class AndroidUtils
{
	public static string GetFileText(string path)
	{
		Debug.Log($"[AndroidUtils.GetFileText] Path: {path}");

		try
		{
			if (path.Contains("://") || path.Contains(":///"))
			{
				var www = new WWW(path);
				while (!www.isDone) { }
				return www.text.Trim();
			}

			return File.ReadAllText(path);
		}
		finally
		{
			
		}
	}
}
