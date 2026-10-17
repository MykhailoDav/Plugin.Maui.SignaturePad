using System.Windows.Input;

namespace Plugin.Maui.SignaturePad;

public partial class SignaturePadView
{
    /// <summary>Undoes the last stroke or clear; it can execute when <see cref="CanUndo"/> is true.</summary>
    public ICommand UndoCommand { get; }

    /// <summary>Redoes what was undone; it can execute when <see cref="CanRedo"/> is true.</summary>
    public ICommand RedoCommand { get; }

    /// <summary>Clears the pad; it can execute when the pad is not empty.</summary>
    public ICommand ClearCommand { get; }

    /// <summary>
    /// Removes the last stroke, or brings back the strokes removed by <see cref="Clear"/>. A stroke being drawn is
    /// finished first, so it is what gets undone.
    /// </summary>
    /// <returns>Whether there was something to undo.</returns>
    public bool Undo() => Session.Undo();

    /// <summary>Applies again what <see cref="Undo"/> took back.</summary>
    /// <returns>Whether there was something to redo.</returns>
    public bool Redo() => Session.Redo();

    /// <summary>Removes every stroke. <see cref="Undo"/> brings them back.</summary>
    public void Clear() => Session.Clear();
}
