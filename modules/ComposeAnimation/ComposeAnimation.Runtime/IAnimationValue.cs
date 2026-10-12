using System.ComponentModel;

namespace ComposeAnimation.Runtime;

/// <summary>A read-only animation sample with notifications on its owning thread.</summary>
public interface IAnimationValue<out T> : INotifyPropertyChanged
{
    T Value { get; }
}
