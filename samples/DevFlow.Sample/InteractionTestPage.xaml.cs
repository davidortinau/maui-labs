using System.Windows.Input;

namespace DevFlow.Sample;

public partial class InteractionTestPage : ContentPage
{
    public ICommand TapCommand { get; }

    public InteractionTestPage()
    {
        TapCommand = new Command<string>(param =>
            SetStatus($"Command tap: {param}"));

        BindingContext = this;
        InitializeComponent();
        TryAttachLongPressRecognizer();
    }

    private void SetStatus(string action)
    {
        StatusLabel.Text = $"last action: {action}";
    }

    private void OnToolbarAction1(object? sender, EventArgs e)
        => SetStatus("toolbar: Action1");

    private void OnToolbarAction2(object? sender, EventArgs e)
        => SetStatus("toolbar: Action2");

    private void OnEventTapGridTapped(object? sender, TappedEventArgs e)
        => SetStatus("event tap: EventTapGrid");

    private void OnLongPressGridTapped(object? sender, TappedEventArgs e)
        => SetStatus("tap: LongPressGrid");

    // LongPressGestureRecognizer is newer than this sample's MAUI floor; attach it via
    // reflection so the positive long-press path lights up automatically once MAUI has it.
    private void TryAttachLongPressRecognizer()
    {
        var type = Type.GetType("Microsoft.Maui.Controls.LongPressGestureRecognizer, Microsoft.Maui.Controls");
        if (type == null || Activator.CreateInstance(type) is not IGestureRecognizer recognizer)
            return;

        var lpEvent = type.GetEvent("LongPressed");
        if (lpEvent?.EventHandlerType == null)
            return;

        EventHandler handler = (_, _) => SetStatus("long press: LongPressGrid");
        lpEvent.AddEventHandler(recognizer, Delegate.CreateDelegate(lpEvent.EventHandlerType, handler.Target, handler.Method));
        LongPressGrid.GestureRecognizers.Add(recognizer);
    }

    private void OnImageButtonClicked(object? sender, EventArgs e)
        => SetStatus("image button clicked");

    private void OnPickerChanged(object? sender, EventArgs e)
        => SetStatus($"picker: {TestPicker.SelectedItem}");

    private void OnTestButtonClicked(object? sender, EventArgs e)
        => SetStatus("button: TestButton");

    private void OnCheckBoxChanged(object? sender, CheckedChangedEventArgs e)
        => SetStatus($"checkbox: {e.Value}");

    private void OnSwitchToggled(object? sender, ToggledEventArgs e)
        => SetStatus($"switch: {e.Value}");
}
