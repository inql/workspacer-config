// Development
// #r "C:\Users\dalyisaac\Repos\workspacer\src\workspacer.Shared\bin\Debug\net5.0-windows\win10-x64\workspacer.Shared.dll"
// #r "C:\Users\dalyisaac\Repos\workspacer\src\workspacer.Bar\bin\Debug\net5.0-windows\win10-x64\workspacer.Bar.dll"
// #r "C:\Users\dalyisaac\Repos\workspacer\src\workspacer.Gap\bin\Debug\net5.0-windows\win10-x64\workspacer.Gap.dll"
// #r "C:\Users\dalyisaac\Repos\workspacer\src\workspacer.FocusIndicator\bin\Debug\net5.0-windows\win10-x64\workspacer.FocusIndicator.dll"


// Production
#r "C:\Program Files\workspacer\workspacer.Shared.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.Bar\workspacer.Bar.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.Gap\workspacer.Gap.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.FocusIndicator\workspacer.FocusIndicator.dll"

using System;
using System.Collections.Generic;
using System.Linq;
using workspacer;
using workspacer.Bar;
using workspacer.Bar.Widgets;
using workspacer.Gap;
using workspacer.FocusIndicator;


// Only 1 screen available
void UltrawideSetup(IConfigContext context)
{

}

// 2 or more screen available, focus on 1 screen but adjust for extra workspace
void MultimonitorSetup(IConfigContext context)
{

}

return new Action<IConfigContext>((IConfigContext context) =>
{
    /* Variables */
    var fontSize = 9;
    var barHeight = 30;
    var fontName = "Cascadia Code PL";
    var background = new Color(0x0, 0x0, 0x0);

    /* Config */
    context.CanMinimizeWindows = true;

    /* Gap */
    var gap = barHeight - 8;
    var gapPlugin = context.AddGap(new GapPluginConfig() { InnerGap = gap, OuterGap = gap / 2, Delta = gap / 2 });

    /* Bar */
    context.AddBar(new BarPluginConfig()
    {
        FontSize = fontSize,
        BarHeight = barHeight,
        FontName = fontName,
        DefaultWidgetBackground = background,
        LeftWidgets = () => new IBarWidget[]
        {
            new WorkspaceWidget(), new TextWidget(": "), new TitleWidget() {
                IsShortTitle = true
            }
        },
        RightWidgets = () => new IBarWidget[]
        {
            new TimeWidget(1000, "hh:mm:ss tt dd-MMM-yyyy"),
            new ActiveLayoutWidget(),
        }
    });

    /* Bar focus indicator */
    context.AddFocusIndicator(new FocusIndicatorPluginConfig()
    {
        BorderColor = Color.Red,
        TimeToShow = 1000, //
    });
    /* Default layouts */
    Func<ILayoutEngine[]> defaultLayouts = () => new ILayoutEngine[]
    {
        new TallLayoutEngine(),
        new VertLayoutEngine(),
        new HorzLayoutEngine(),
        new FullLayoutEngine(),
    };

    context.DefaultLayouts = defaultLayouts;

    var monitors = context.MonitorContainer.GetAllMonitors();

    var sticky = new StickyWorkspaceContainer(context, StickyWorkspaceIndexMode.Local);
    context.WorkspaceContainer = sticky;

    var mainWorkspaces = new Dictionary<string, ILayoutEngine[]> {
        {"1|main", new ILayoutEngine[] { new DwindleLayoutEngine()}},
        {"2|secondary", new ILayoutEngine[] { new FocusLayoutEngine ()}},
        {"3|term", defaultLayouts()},
        {"4|comm", defaultLayouts()},
        {"5|org", defaultLayouts()},
        {"6|offtopic", defaultLayouts()},
        {"7|full", defaultLayouts()},
        {"8|media🎶", defaultLayouts()}
    };

    var mainWorkspacesNames = mainWorkspaces.Keys.ToList();

    foreach (var item in mainWorkspaces)
    {
        sticky.CreateWorkspace(monitors[0], item.Key, item.Value);
    }

    /* TODO: define cool stuff for rest of the monitors */
    for (int i = 1; i < monitors.Length; i++)
    {
        sticky.CreateWorkspaces(monitors[i], $"{i}:1", $"{i}:2", $"{i}:3", $"{i}:4", $"{i}:5", $"{i}:6", $"{i}:7", $"{i}:8", $"{i}:9");
    }

    /* Routes */

    var routeMapper = new Dictionary<string, List<string>>()
    {
        {mainWorkspacesNames[0], new List<string> {}},
        {mainWorkspacesNames[1], new List<string> {}},
        {mainWorkspacesNames[2], new List<string> {}},
        {mainWorkspacesNames[3], new List<string> {"Microsoft Teams"}},
        {mainWorkspacesNames[4], new List<string> {"Outlook"}},
        {mainWorkspacesNames[5], new List<string> {"Discord"}},
        {mainWorkspacesNames[6], new List<string> {}},
        {mainWorkspacesNames[7], new List<string> {"Spotify"}}
    };

    var rejectList = new List<string>(){
        "1Password.exe",
        "pinentry.exe",
        "Volume Mixer"
    };

    foreach (var workspace in routeMapper.Keys)
    {
        foreach (var process in routeMapper[workspace])
        {
            context.WindowRouter.AddRoute((window) => window.Title.Contains(process) ? context.WorkspaceContainer[workspace] : null);
        }
    }

    foreach (var process in rejectList)
    {
        context.WindowRouter.AddFilter((window) => !window.ProcessFileName.Equals(process));
        context.WindowRouter.AddFilter((window) => !window.Title.Contains(process));
    }

    /* Keybindings */
    Action setKeybindings = () =>
    {
        KeyModifiers winShift = KeyModifiers.Win | KeyModifiers.Shift;
        KeyModifiers winCtrl = KeyModifiers.Win | KeyModifiers.Control;
        KeyModifiers win = KeyModifiers.Win;

        IKeybindManager manager = context.Keybinds;

        var workspaces = context.Workspaces;

        // manager.UnsubscribeAll();
        manager.Subscribe(MouseEvent.LButtonDown, () => workspaces.SwitchFocusedMonitorToMouseLocation());

        // Left, Right keys
        manager.Subscribe(winCtrl, Keys.Left, () => workspaces.SwitchToPreviousWorkspace(), "switch to previous workspace");
        manager.Subscribe(winCtrl, Keys.Right, () => workspaces.SwitchToNextWorkspace(), "switch to next workspace");

        manager.Subscribe(winShift, Keys.Left, () => workspaces.MoveFocusedWindowToPreviousMonitor(), "move focused window to previous monitor");
        manager.Subscribe(winShift, Keys.Right, () => workspaces.MoveFocusedWindowToNextMonitor(), "move focused window to next monitor");

        // H, L keys
        manager.Subscribe(winShift, Keys.H, () => workspaces.FocusedWorkspace.ShrinkPrimaryArea(), "shrink primary area");
        manager.Subscribe(winShift, Keys.L, () => workspaces.FocusedWorkspace.ExpandPrimaryArea(), "expand primary area");

        manager.Subscribe(winCtrl, Keys.H, () => workspaces.FocusedWorkspace.DecrementNumberOfPrimaryWindows(), "decrement number of primary windows");
        manager.Subscribe(winCtrl, Keys.L, () => workspaces.FocusedWorkspace.IncrementNumberOfPrimaryWindows(), "increment number of primary windows");

        // K, J keys
        manager.Subscribe(winShift, Keys.K, () => workspaces.FocusedWorkspace.SwapFocusAndNextWindow(), "swap focus and next window");
        manager.Subscribe(winShift, Keys.J, () => workspaces.FocusedWorkspace.SwapFocusAndPreviousWindow(), "swap focus and previous window");

        manager.Subscribe(win, Keys.K, () => workspaces.FocusedWorkspace.FocusNextWindow(), "focus next window");
        manager.Subscribe(win, Keys.J, () => workspaces.FocusedWorkspace.FocusPreviousWindow(), "focus previous window");

        // Add, Subtract keys
        manager.Subscribe(winCtrl, Keys.Add, () => gapPlugin.IncrementInnerGap(), "increment inner gap");
        manager.Subscribe(winCtrl, Keys.Subtract, () => gapPlugin.DecrementInnerGap(), "decrement inner gap");

        manager.Subscribe(winShift, Keys.Add, () => gapPlugin.IncrementOuterGap(), "increment outer gap");
        manager.Subscribe(winShift, Keys.Subtract, () => gapPlugin.DecrementOuterGap(), "decrement outer gap");

        // Other shortcuts
        manager.Subscribe(winShift, Keys.Escape, () => context.Enabled = !context.Enabled, "toggle enabled/disabled");
        manager.Subscribe(winShift, Keys.I, () => context.ToggleConsoleWindow(), "toggle console window");
    };
    setKeybindings();
});
