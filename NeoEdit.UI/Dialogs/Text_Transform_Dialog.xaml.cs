using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Text_Transform_Dialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<Text_Transform_Dialog>.GetPropValue<string>(this); } set { UIHelper<Text_Transform_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Text_Transform_Dialog() { UIHelper<Text_Transform_Dialog>.Register(); }

		Text_Transform_Dialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Expression = "c";
		}

		Configuration_Text_Transform result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Text_Transform { Expression = Expression };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		public static Configuration_Text_Transform Run(Window parent, NEVariables variables)
		{
			var dialog = new Text_Transform_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
