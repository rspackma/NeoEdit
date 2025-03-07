﻿using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace Build
{
	partial class PasswordDialog
	{
		PasswordDialog()
		{
			InitializeComponent();
		}

		string decoded;
		void OkClick(object sender, RoutedEventArgs e)
		{
			try
			{
				var data = Convert.FromBase64String("DyUoIELBonM2CUdSV+16gy8krNYc0n5D+4UfV8LJiFlNCtt+oXWK0cZXN320+o9aE3wmJ2aGw/0/rd/txZ3ksw==");
				using (var alg = new AesCryptoServiceProvider())
				{
					alg.IV = Convert.FromBase64String("7gR10RNIeruIGYAFpIIKXg==");
					using (var byteGenerator = new Rfc2898DeriveBytes(password.Password, Convert.FromBase64String("RYRwx+Bu9dLtKY6NaQEgyRNiYsG59MHv")))
						alg.Key = Convert.FromBase64String(Convert.ToBase64String(byteGenerator.GetBytes(32)));

					using (var decryptor = alg.CreateDecryptor())
						decoded = Encoding.UTF8.GetString(decryptor.TransformFinalBlock(data, 0, data.Length));

					if ((decoded.StartsWith("token:")) && (decoded.EndsWith(":token")))
					{
						decoded = decoded.Substring("token:".Length, decoded.Length - "token:".Length * 2);
						DialogResult = true;
						return;
					}
				}
			}
			catch { }
			MessageBox.Show("Invalid Password", "Error");
		}

		public static string Run()
		{
			var dialog = new PasswordDialog();
			if (dialog.ShowDialog() != true)
				return null;
			return dialog.decoded;
		}
	}
}
