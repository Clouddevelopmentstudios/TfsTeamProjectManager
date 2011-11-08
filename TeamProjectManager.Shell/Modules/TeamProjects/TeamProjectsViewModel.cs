﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Windows;
using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;
using TeamProjectManager.Shell.Infrastructure;

namespace TeamProjectManager.Shell.Modules.TeamProjects
{
    [Export]
    public class TeamProjectsViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand AddTeamProjectCollectionCommand { get; private set; }

        #endregion

        #region Observable Properties

        public IEnumerable<RegisteredProjectCollection> TfsTeamProjectCollections
        {
            get { return this.GetValue(TfsTeamProjectCollectionsProperty); }
            set { this.SetValue(TfsTeamProjectCollectionsProperty, value); }
        }

        public static ObservableProperty<IEnumerable<RegisteredProjectCollection>> TfsTeamProjectCollectionsProperty = new ObservableProperty<IEnumerable<RegisteredProjectCollection>, TeamProjectsViewModel>(o => o.TfsTeamProjectCollections);

        [Export]
        public RegisteredProjectCollection SelectedTfsTeamProjectCollection
        {
            get { return this.GetValue(SelectedTfsTeamProjectCollectionProperty); }
            set { this.SetValue(SelectedTfsTeamProjectCollectionProperty, value); }
        }

        public static ObservableProperty<RegisteredProjectCollection> SelectedTfsTeamProjectCollectionProperty = new ObservableProperty<RegisteredProjectCollection, TeamProjectsViewModel>(o => o.SelectedTfsTeamProjectCollection, null, OnSelectedTfsTeamProjectCollectionChanged);

        [Export]
        public IEnumerable<ProjectInfo> TfsTeamProjects
        {
            get { return this.GetValue(TfsTeamProjectsProperty); }
            set { this.SetValue(TfsTeamProjectsProperty, value); }
        }

        public static ObservableProperty<IEnumerable<ProjectInfo>> TfsTeamProjectsProperty = new ObservableProperty<IEnumerable<ProjectInfo>, TeamProjectsViewModel>(o => o.TfsTeamProjects, OnTfsTeamProjectsChanged);

        public ICollection<ProjectInfo> SelectedTfsTeamProjects
        {
            get { return this.GetValue(SelectedTfsTeamProjectsProperty); }
            set { this.SetValue(SelectedTfsTeamProjectsProperty, value); }
        }

        public static ObservableProperty<ICollection<ProjectInfo>> SelectedTfsTeamProjectsProperty = new ObservableProperty<ICollection<ProjectInfo>, TeamProjectsViewModel>(o => o.SelectedTfsTeamProjects, null, OnSelectedTfsTeamProjectsChanged);

        public Visibility TeamProjectsVisibility
        {
            get { return this.GetValue(TeamProjectsVisibilityProperty); }
            set { this.SetValue(TeamProjectsVisibilityProperty, value); }
        }

        public static ObservableProperty<Visibility> TeamProjectsVisibilityProperty = new ObservableProperty<Visibility, TeamProjectsViewModel>(o => o.TeamProjectsVisibility);

        public Visibility InfoMessageVisibility
        {
            get { return this.GetValue(InfoMessageVisibilityProperty); }
            set { this.SetValue(InfoMessageVisibilityProperty, value); }
        }

        public static ObservableProperty<Visibility> InfoMessageVisibilityProperty = new ObservableProperty<Visibility, TeamProjectsViewModel>(o => o.InfoMessageVisibility);

        public string InfoMessage
        {
            get { return this.GetValue(InfoMessageProperty); }
            set { this.SetValue(InfoMessageProperty, value); }
        }

        public static ObservableProperty<string> InfoMessageProperty = new ObservableProperty<string, TeamProjectsViewModel>(o => o.InfoMessage);

        public bool IsTeamProjectsLoadComplete
        {
            get { return this.GetValue(IsTeamProjectsLoadCompleteProperty); }
            set { this.SetValue(IsTeamProjectsLoadCompleteProperty, value); }
        }

        public static ObservableProperty<bool> IsTeamProjectsLoadCompleteProperty = new ObservableProperty<bool, TeamProjectsViewModel>(o => o.IsTeamProjectsLoadComplete, true);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public TeamProjectsViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base("Team Projects", eventAggregator, logger)
        {
            this.AddTeamProjectCollectionCommand = new RelayCommand(AddTeamProjectCollection);
            RefreshTeamProjectCollections(null);
        }

        #endregion

        #region Property Change Handlers

        private static void OnTfsTeamProjectsChanged(ObservableObject sender, ObservablePropertyChangedEventArgs<IEnumerable<ProjectInfo>> e)
        {
            var viewModel = (TeamProjectsViewModel)sender;
            viewModel.SelectedTfsTeamProjects = null;
        }

        private static void OnSelectedTfsTeamProjectCollectionChanged(ObservableObject sender, ObservablePropertyChangedEventArgs<RegisteredProjectCollection> e)
        {
            var viewModel = (TeamProjectsViewModel)sender;
            viewModel.TfsTeamProjects = null;
            if (viewModel.SelectedTfsTeamProjectCollection != null)
            {
                var task = new ApplicationTask(string.Format(CultureInfo.CurrentCulture, "Retrieving team projects for \"{0}\"", viewModel.SelectedTfsTeamProjectCollection.Name));
                viewModel.SetInfoMessage("Loading...");
                viewModel.PublishStatus(new StatusEventArgs(task));
                viewModel.IsTeamProjectsLoadComplete = false;
                var worker = new BackgroundWorker();
                worker.DoWork += (bsender, be) =>
                {
                    using (var tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(viewModel.SelectedTfsTeamProjectCollection.Uri))
                    {
                        var store = tfs.GetService<ICommonStructureService>();
                        be.Result = store.ListAllProjects().OrderBy(p => p.Name);
                    }
                };
                worker.RunWorkerCompleted += (bsender, be) =>
                {
                    if (be.Error != null)
                    {
                        viewModel.Logger.Log("An unexpected exception occurred while retrieving team projects", be.Error);
                        task.SetError(be.Error);
                        task.SetComplete("An unexpected exception occurred");
                    }
                    else
                    {
                        viewModel.TfsTeamProjects = (IEnumerable<ProjectInfo>)be.Result;
                        task.SetComplete("Retrieved " + viewModel.TfsTeamProjects.Count().ToCountString("team project"));
                    }
                    viewModel.ClearInfoMessage();
                    viewModel.IsTeamProjectsLoadComplete = true;
                };
                worker.RunWorkerAsync();
            }
        }

        private static void OnSelectedTfsTeamProjectsChanged(ObservableObject sender, ObservablePropertyChangedEventArgs<ICollection<ProjectInfo>> e)
        {
            var viewModel = (TeamProjectsViewModel)sender;
            var selectedTeamProjectCollection = viewModel.SelectedTfsTeamProjectCollection == null ? null : new TeamProjectCollectionInfo(viewModel.SelectedTfsTeamProjectCollection.Name, viewModel.SelectedTfsTeamProjectCollection.Uri);
            var selectedTeamProjects = viewModel.SelectedTfsTeamProjects == null ? null : viewModel.SelectedTfsTeamProjects.Select(p => new TeamProjectInfo(p.Name, new Uri(p.Uri))).ToList();
            viewModel.EventAggregator.GetEvent<TeamProjectSelectionChangedEvent>().Publish(new TeamProjectSelectionChangedEventArgs(selectedTeamProjectCollection, selectedTeamProjects));
        }

        #endregion

        #region Commands

        private void AddTeamProjectCollection(object argument)
        {
            using (var dialog = new TeamProjectPicker(TeamProjectPickerMode.NoProject, false))
            {
                var result = dialog.ShowDialog(Application.Current.MainWindow.GetIWin32Window());
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var projectCollection = dialog.SelectedTeamProjectCollection;
                    RegisteredTfsConnections.RegisterProjectCollection(projectCollection);
                    RefreshTeamProjectCollections(projectCollection.Name);
                }
            }
        }

        #endregion

        #region Helper Methods

        private void RefreshTeamProjectCollections(string selectedTeamProjectCollectionName)
        {
            try
            {
                SetInfoMessage("Loading...");
                this.TfsTeamProjectCollections = RegisteredTfsConnections.GetProjectCollections();
                this.SelectedTfsTeamProjectCollection = this.TfsTeamProjectCollections.FirstOrDefault(t => t.Name == selectedTeamProjectCollectionName);
                SetInfoMessage("Please select a project collection");
            }
            catch (Exception exc)
            {
                Logger.Log("An unexpected exception occurred while retrieving the Team Project Collections", exc);
            }
        }

        private void SetInfoMessage(string infoMessage)
        {
            this.InfoMessage = infoMessage;
            this.InfoMessageVisibility = string.IsNullOrWhiteSpace(infoMessage) ? Visibility.Hidden : Visibility.Visible;
            this.TeamProjectsVisibility = string.IsNullOrWhiteSpace(infoMessage) ? Visibility.Visible : Visibility.Hidden;
        }

        private void ClearInfoMessage()
        {
            SetInfoMessage(null);
        }

        #endregion
    }
}