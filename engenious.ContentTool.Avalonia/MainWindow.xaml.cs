using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Dialogs;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Dialogs.Avalonia;
using engenious.ContentTool.Forms;
using engenious.ContentTool.Models;
using engenious.ContentTool.Viewer;
using engenious.Graphics;

namespace engenious.ContentTool.Avalonia
{
    public class MainWindow : global::Avalonia.Controls.Window, IMainShell
    {
        public static readonly DirectProperty<MainWindow, ContentProject> ProjectProperty =
            AvaloniaProperty.RegisterDirect<MainWindow, ContentProject>(
                nameof(Project),
                o => o.Project,
                (o, v) => o.Project = v);

        public static readonly DirectProperty<MainWindow, bool> IsProjectOpenProperty =
            AvaloniaProperty.RegisterDirect<MainWindow, bool>(
                nameof(IsProjectOpen),
                o => o.IsProjectOpen);

        private readonly App _app;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private ContentProject _project;

        private readonly ProjectTreeView _projectTreeView;
        private readonly IPromptShell _promptShell;

        
        public static readonly DirectProperty<MainWindow, PropertyViewBase> ContentPropertiesProperty =
            AvaloniaProperty.RegisterDirect<MainWindow, PropertyViewBase>(
                nameof(PropertyView),
                o => o.ContentProperties,
                (o, v) => o.ContentProperties = v);

        public PropertyViewBase ContentProperties
        {
            get => _contentProperties;
            set => SetAndRaise(ContentPropertiesProperty, ref _contentProperties, value);
        }

        public MainWindow()
        {

            LogItems =  new ObservableList<LogItem>();
            DataContext = this;
            InitializeComponent();

            _projectTreeView = this.FindControl<ProjectTreeView>("projectTreeView");
            _projectTreeView.SelectedItemChanged +=
                (sender, args) => OnItemSelect?.Invoke(_projectTreeView.SelectedItem);
            _logText = this.FindControl<TextBlock>("logText");
            _defaultTextBlockColorDummy = this.FindControl<TextBlock>("defaultTextBlockColorDummy");

            _promptShell = new PromptShell();
        }
        
        

        public MainWindow(App app)
            : this()
        {
            _app = app;
        }

        public bool IsProjectOpen => Project != null;

        public ContentProject Project
        {
            get => _project;
            set
            {
                if (_project == value)
                    return;

                if (_project != null)
                {
                    var tmp = RootPropertyView.Create("Test", Project, 3);
                    this.FindControl<PropertyGrid>("propertyGrid").PropertyView = tmp;
                    _project.History.HistoryChanged -= HistoryOnHistoryChanged;
                    _project.CollectionChanged -= ProjectOnCollectionChanged;
                }

                var oldProj = _project;

                SetAndRaise(ProjectProperty, ref _project, value);


                RaisePropertyChanged(IsProjectOpenProperty, oldProj != null, IsProjectOpen);


                if (_project != null)
                {
                    _project.History.HistoryChanged += HistoryOnHistoryChanged;
                    _project.CollectionChanged += ProjectOnCollectionChanged;
                }

                _projectTreeView.Project = Project;
            }
        }

        private void ProjectOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }

        private void HistoryOnHistoryChanged(object? sender, EventArgs e)
        {
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _cts.Cancel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void Dispose()
        {
        }

        public void Run()
        {
            // A cancellation token source that will be used to stop the main loop

            // Do you startup code here
            Show();

            // Start the main loop
            _app.Run(_cts.Token);
        }

        public IViewer CurrentViewer { get; private set; }
        public bool IsRenderingSuspended { get; }

        public void Invoke(Action d)
        {
            using var t = Dispatcher.UIThread.InvokeAsync(d);
            t.Wait();
        }

        public void BeginInvoke(Action d)
        {
            Dispatcher.UIThread.InvokeAsync(d);
        }

        public async Task<bool> ShowCloseWithoutSavingConfirmation()
        {
            var result = await _promptShell.ShowMessageBox(
                $"There are unsaved changes. {Environment.NewLine} Do you want to save the project before closing it?",
                "Unsaved changes", MessageBoxButtons.YesNoCancel, MessageBoxType.Warning, this);
            if (result == MessageBoxResult.Cancel)
                return false;
            if (result != MessageBoxResult.Yes) return true;
            if (CurrentViewer != null && CurrentViewer.UnsavedChanges)
                CurrentViewer.Save();
            if (_project.HasUnsavedChanges)
                SaveProjectClick?.Invoke(Project);

            return true;
        }

        public async Task<string> ShowOpenDialog()
        {
            var d = new OpenFileDialog
            {
                Title = "Open Project...",
                AllowMultiple = false,
                Directory = Environment.CurrentDirectory,
                Filters = new List<FileDialogFilter>(new[]
                {
                    new FileDialogFilter() {Name = "Engenious Content Project(.ecp)", Extensions = {"ecp"}}
                })
            };
            var res = await d.ShowAsync(this);
            return res != null && res.Length > 0 ? res[0] : null;
        }

        public async Task<string> ShowSaveAsDialog()
        {
            var d = new SaveFileDialog
            {
                Title = "Save Project as...",
                Directory = Environment.CurrentDirectory,
                Filters = new List<FileDialogFilter>(new[]
                {
                    new FileDialogFilter() {Name = "Engenious Content Project(.ecp)", Extensions = {"ecp"}}
                })
            };
            var res = await d.ShowAsync(this);
            return res;
        }

        public async Task<string> ShowIntegrateDialog()
        {
            throw new NotImplementedException();
        }

        public static readonly DirectProperty<MainWindow, bool> LoadingShownProperty =
            AvaloniaProperty.RegisterDirect<MainWindow, bool>
                (nameof(LoadingShown), window => window.LoadingShown, (window, v) => window.LoadingShown = v);

        public static readonly DirectProperty<MainWindow, bool> IsProgressIndeterminateProperty =
            AvaloniaProperty.RegisterDirect<MainWindow, bool>
            (nameof(IsProgressIndeterminate), window => window.IsProgressIndeterminate,
                (window, v) => window.IsProgressIndeterminate = v);

        public static readonly DirectProperty<MainWindow, int> ProgressValueProperty =
            AvaloniaProperty.RegisterDirect<MainWindow, int>
                (nameof(ProgressValue), window => window.ProgressValue, (window, v) => window.ProgressValue = v);

        private bool _loadingShown;

        public bool LoadingShown
        {
            get => _loadingShown;
            set
            {
                if (value)
                    IsProgressIndeterminate = true;
                SetAndRaise(LoadingShownProperty, ref _loadingShown, value);
            }
        }

        private bool _isProgressIndeterminate;

        public bool IsProgressIndeterminate
        {
            get => _isProgressIndeterminate;
            set => SetAndRaise(LoadingShownProperty, ref _isProgressIndeterminate, value);
        }

        private int _progressValue;
        private TextBlock _logText;

        public int ProgressValue
        {
            get => _progressValue;
            set
            {
                IsProgressIndeterminate = false;
                SetAndRaise(ProgressValueProperty, ref _progressValue, value);
            }
        }

        public async Task ShowLoading(string title = "Please wait...")
        {
            LoadingShown = true;
        }

        public async Task HideLoading()
        {
            LoadingShown = false;
        }
        public static readonly DirectProperty<MainWindow, Control> CurrentPreviewControlProperty =
            AvaloniaProperty.RegisterDirect<MainWindow, Control>(
                nameof(CurrentPreviewControl),
                o => o.CurrentPreviewControl,
                (o, v) => o.CurrentPreviewControl = v);
        public Control CurrentPreviewControl
        {
            get => _currentPreviewControl;
            set => SetAndRaise(CurrentPreviewControlProperty, ref _currentPreviewControl, value);
        }

        public async Task ShowViewer(IViewer viewer, ContentFile file)
        {
            if (CurrentViewer != null)
            {
                if (CurrentViewer.UnsavedChanges)
                {
                    if (await _promptShell.ShowMessageBox(
                        $"This file '{CurrentViewer.ContentFile.Name}' has unsaved changes. Do you want to save them?",
                        "Save changes?", MessageBoxButtons.YesNo,
                        MessageBoxType.Warning, this) == MessageBoxResult.Yes)
                        CurrentViewer.Save();
                    else
                        CurrentViewer.Discard();
                }
                if (CurrentViewer.History != null)
                {
                    _project.History.Remove(CurrentViewer.History);
                }
            }

            CurrentViewer = viewer?.GetViewerControl(file) as IViewer;
            
            CurrentPreviewControl = CurrentViewer as Control;
        }

        public async Task HideViewer()
        {
            CurrentPreviewControl = null;
        }

        public async Task RenameItem(ContentItem item)
        {
            throw new NotImplementedException();
        }

        public async Task RemoveItem(ContentItem item)
        {
            throw new NotImplementedException();
        }

        public async Task ShowAbout()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ShowNotFoundDelete()
        {
            return await _promptShell.ShowMessageBox(
                "This file could not be found. " + Environment.NewLine +
                "Do you want to remove it from the Project?", "File not found!", MessageBoxButtons.YesNo,
                MessageBoxType.Warning, this) == MessageBoxResult.Yes;
        }

        public async Task ReloadView()
        {
            throw new NotImplementedException();
        }

        public async Task WaitProgress(int progress)
        {
            ProgressValue = progress;
        }

        public async Task SuspendRendering()
        {
        }

        public async Task ResumeRendering()
        {
        }

        public async Task<string> ShowFolderSelectDialog()
        {
            var d = new OpenFolderDialog {Title = "Select folder..."};
            var res = await d.ShowAsync(this);
            return res;
        }

        public async Task<string[]> ShowFileSelectDialog()
        {
            var d = new OpenFileDialog {Title = "Select files...", AllowMultiple = false};
            var res = await d.ShowAsync(this);
            return res;
        }

        public event EventHandler ViewReloaded;
        public event EventHandler UndoClick;
        public event EventHandler RedoClick;
        public event Delegates.ItemActionEventHandler BuildItemClick;
        public event Delegates.ItemActionEventHandler ShowInExplorerItemClick;
        public event Delegates.ItemActionEventHandler RemoveItemClick;
        public event Delegates.ItemActionEventHandler RenameItemClick;
        public event EventHandler OnShellLoad;
        public event EventHandler RebuildClick;
        public event EventHandler CleanClick;
        public event Delegates.FolderAddActionEventHandler AddExistingFolderClick;
        public event Delegates.FolderAddActionEventHandler AddNewFolderClick;
        public event Delegates.FolderAddActionEventHandler AddExistingItemClick;
        public event EventHandler NewProjectClick;
        public event EventHandler OpenProjectClick;
        public event Delegates.ItemActionEventHandler CloseProjectClick;
        public event Delegates.ItemActionEventHandler SaveProjectClick;
        public event Delegates.ItemActionEventHandler SaveProjectAsClick;
        public event Delegates.ItemActionEventHandler OnItemSelect;
        public event EventHandler IntegrateCSClick;
        public event EventHandler OnAboutClick;
        public event EventHandler OnHelpClick;

        public event EventHandler<(GraphicsDevice graphicsDevice, IRenderingSurface renderingSurface)>
            CreateGraphicsContext;


        public struct LogItem
        {
            public LogItem(string text, IBrush color)
            {
                Text = text;
                Color = color;
            }
            public string Text { get; }
            public IBrush Color { get; }
        }
        
        public ObservableList<LogItem> LogItems { get; }

        private IBrush ToBrush(Color color)
        {
            if (color == default)
                return _defaultTextBlockColorDummy.Foreground;
            else
                return new SolidColorBrush(new global::Avalonia.Media.Color(
                    (byte) Math.Clamp((int) color.A * 255, 0, 255),
                    (byte) Math.Clamp((int) color.R * 255, 0, 255),
                    (byte) Math.Clamp((int) color.G * 255, 0, 255),
                    (byte) Math.Clamp((int) color.B * 255, 0, 255)));
        }

        public async Task WriteLineLog(string text, Color color = default)
        {
            LogItems.Add(new LogItem(text, ToBrush(color)));
        }

        public async Task WriteLog(string text, Color color = default)
        {
            LogItems.Add(new LogItem(text, ToBrush(color)));
        }

        public async Task ClearLog()
        {
            LogItems.Clear();
        }

        public static readonly DirectProperty<MainWindow, bool> AlwaysShowLogProperty =
            AvaloniaProperty.RegisterDirect<MainWindow, bool>
            (nameof(AlwaysShowLog), window => window.AlwaysShowLog,
                (window, v) => window.AlwaysShowLog = v);

        public static readonly DirectProperty<MainWindow, bool> LogShownProperty =
            AvaloniaProperty.RegisterDirect<MainWindow, bool>
            (nameof(LogShown), window => window.LogShown,
                (window, v) => window.LogShown = v);

        public static readonly DirectProperty<MainWindow, int> LogRowSpanProperty =
            AvaloniaProperty.RegisterDirect<MainWindow, int>
                (nameof(LogRowSpan), window => window.LogRowSpan);

        public int LogRowSpan => LogShown ? 1 : 3;

        private bool _alwaysShowLog;

        public bool AlwaysShowLog
        {
            get => _alwaysShowLog;
            set
            {
                var oldRowSpan = LogRowSpan;
                SetAndRaise(AlwaysShowLogProperty, ref _alwaysShowLog, value);
                RaisePropertyChanged(LogShownProperty, _logShown, LogShown);
                RaisePropertyChanged(LogRowSpanProperty, oldRowSpan, LogRowSpan);
            }
        }


        private bool _logShown;
        private TextBlock _defaultTextBlockColorDummy;
        private Control _currentPreviewControl;
        private PropertyViewBase _contentProperties;

        public bool LogShown
        {
            get => _logShown || AlwaysShowLog;
            set
            {
                var oldRowSpan = LogRowSpan;
                SetAndRaise(LogShownProperty, ref _logShown, value);
                RaisePropertyChanged(LogRowSpanProperty, oldRowSpan, LogRowSpan);
            }
        }

        public async Task ShowLog()
        {
            Project.OutputDirectory = "bla";
            LogShown = true;
        }

        public async Task HideLog()
        {
            LogShown = false;
        }

        private void AlwaysShowLogClicked(object sender, RoutedEventArgs e)
        {
            AlwaysShowLog = !AlwaysShowLog;
        }

        private void OnOpenProjectButtonClick(object sender, RoutedEventArgs e) =>
            OpenProjectClick?.Invoke(sender, EventArgs.Empty);

        private void OnNewProjectButtonClick(object sender, RoutedEventArgs e) =>
            NewProjectClick?.Invoke(sender, EventArgs.Empty);

        private void OnSaveProjectButtonClick(object sender, RoutedEventArgs e) =>
            SaveProjectClick?.Invoke(Project);

        private void OnSaveProjectAsButtonClick(object sender, RoutedEventArgs e) =>
            SaveProjectAsClick?.Invoke(Project);

        private void OnCloseProjectButtonClick(object sender, RoutedEventArgs e) =>
            CloseProjectClick?.Invoke(Project);

        private void OnExitButtonClick(object sender, RoutedEventArgs e)
        {
            CloseProjectClick?.Invoke(Project);
            Close();
            _cts.Cancel();
        }

        private void OnUndoButtonClick(object sender, RoutedEventArgs e) =>
            UndoClick?.Invoke(sender, EventArgs.Empty);

        private void OnRedoButtonClick(object sender, RoutedEventArgs e) =>
            RedoClick?.Invoke(sender, EventArgs.Empty);

        private void OnNewItemButtonClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnNewFolderButtonClick(object sender, RoutedEventArgs e) =>
            AddNewFolderClick?.Invoke(_projectTreeView.SelectedFolder);

        private void OnExistingItemButtonClick(object sender, RoutedEventArgs e) =>
            AddExistingItemClick?.Invoke(_projectTreeView.SelectedFolder);

        private void OnExistingFolderButtonClick(object sender, RoutedEventArgs e) =>
            AddExistingFolderClick?.Invoke(_projectTreeView.SelectedFolder);

        private void OnRenameItemButtonClick(object sender, RoutedEventArgs e) =>
            RenameItemClick?.Invoke(_projectTreeView.SelectedItem);

        private void OnRemoveItemButtonClick(object sender, RoutedEventArgs e) =>
            RemoveItemClick?.Invoke(_projectTreeView.SelectedItem);

        private void OnBuildButtonClick(object sender, RoutedEventArgs e) =>
            BuildItemClick?.Invoke(_projectTreeView.SelectedItem ?? Project);

        private void OnCleanButtonClick(object sender, RoutedEventArgs e) =>
            CleanClick?.Invoke(sender, EventArgs.Empty);

        private void OnRebuildButtonClick(object sender, RoutedEventArgs e) =>
            RebuildClick?.Invoke(sender, EventArgs.Empty);

        private async void FormClosing(object sender, CancelEventArgs e)
        {
            if (Project == null ||
                !Project.HasUnsavedChanges && !(CurrentViewer != null && CurrentViewer.UnsavedChanges)) return;
            if (!await ShowCloseWithoutSavingConfirmation())
                e.Cancel = true;
        }
    }
}