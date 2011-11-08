﻿using System.ComponentModel.Composition;
using System.Windows.Shell;
using Microsoft.Practices.Prism.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;
using TeamProjectManager.Shell.Infrastructure;

namespace TeamProjectManager.Shell
{
    [Export(typeof(ShellViewModel))]
    [Export(typeof(IStatusService))]
    public class ShellViewModel : ViewModelBase, IStatusService
    {
        #region Constants

        private const string DefaultWindowTitle = "Team Project Manager";

        #endregion

        #region Properties

        public string HeaderTitle { get; private set; }
        public string HeaderSubtitle { get; private set; }
        public TaskbarItemInfo TaskbarItemInfo { get; private set; }

        #endregion

        #region Observable Properties

        public string WindowTitle
        {
            get { return this.GetValue(WindowTitleProperty); }
            set { this.SetValue(WindowTitleProperty, value); }
        }

        public static ObservableProperty<string> WindowTitleProperty = new ObservableProperty<string, ShellViewModel>(o => o.WindowTitle);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public ShellViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base("Shell", eventAggregator, logger)
        {
            this.HeaderTitle = DefaultWindowTitle;
            this.HeaderSubtitle = "v" + App.ApplicationVersion.ToString(3);
            this.WindowTitle = DefaultWindowTitle;
            this.TaskbarItemInfo = new TaskbarItemInfo();
        }

        #endregion

        #region IStatusService Members

        public void SetMainWindowTitleStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                this.WindowTitle = DefaultWindowTitle;
            }
            else
            {
                this.WindowTitle = string.Concat(DefaultWindowTitle, " - ", status);
            }
        }

        public void ClearMainWindowTitleStatus()
        {
            SetMainWindowTitleStatus(null);
        }

        public void SetMainWindowProgress(TaskbarItemProgressState state, double progressValue)
        {
            this.TaskbarItemInfo.ProgressState = state;
            this.TaskbarItemInfo.ProgressValue = progressValue;
        }

        public void SetMainWindowProgress(TaskbarItemProgressState state)
        {
            this.TaskbarItemInfo.ProgressState = state;
        }

        #endregion
    }
}