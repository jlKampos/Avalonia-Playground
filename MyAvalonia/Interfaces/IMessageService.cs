using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MyAvalonia.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace MyAvalonia.Interfaces
{
	public interface IMessageService
	{
		Task ShowAsync(string message, MessageDialogType type);
	}
}
