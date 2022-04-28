﻿using Microsoft.JSInterop;

namespace BlazorComponent;

public abstract class TransitionElementBase : Element
{
    /// <summary>
    /// The dom information about the transitional element.
    /// </summary>
    internal BlazorComponent.Web.Element? ElementInfo { get; set; }
}

public abstract class TransitionElementBase<TValue> : TransitionElementBase, IAsyncDisposable
{
    [Inject]
    protected IJSRuntime Js { get; set; }

    [CascadingParameter]
    public Transition? Transition { get; set; }

    [Parameter]
    public TValue Value { get; set; }

    private TValue _preValue;
    private TransitionJsInvoker? _transitionJsInvoker;
    private bool _transitionRunning;

    protected bool FirstRender { get; private set; } = true;

    protected abstract TransitionState CurrentState { get; }

    /// <summary>
    /// Whether it is a transitional element.
    /// </summary>
    protected bool HavingTransition => !string.IsNullOrWhiteSpace(Transition?.Name) && Transition?.TransitionElement == this;

    /// <summary>
    /// No transition or is not a transitional element.
    /// </summary>
    protected bool NoTransition => !HavingTransition;

    protected override void OnInitialized()
    {
        if (Transition is not null && Transition.TransitionElement is null)
        {
            Transition.TransitionElement = this;
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (NoTransition)
        {
            return;
        }

        if (!EqualityComparer<TValue>.Default.Equals(Value, _preValue))
        {
            _preValue = Value;

            StartTransition();

            _transitionRunning = true;
        }

        // hooks
        // TODO: but it hasn't been tested yet
        if (_transitionRunning)
        {
            switch (CurrentState)
            {
                case TransitionState.None:
                    if (Transition!.Mode is TransitionMode.InOut)
                    {
                        await Transition!.BeforeEnter(this);
                    }
                    else
                    {
                        await Transition!.BeforeLeave(this);
                    }

                    break;
                case TransitionState.Leave:
                    await Transition!.Leave(this);

                    break;
                case TransitionState.Enter:
                    Console.WriteLine("transition state: enter");
                    await Transition!.Enter(this);

                    break;
                case TransitionState.EnterTo:
                    if (Value is true || Transition!.Mode is TransitionMode.OutIn)
                    {
                        _transitionRunning = false;
                    }

                    await Transition!.AfterEnter(this);

                    break;
                case TransitionState.LeaveTo:
                    if (Value is false || Transition!.Mode is TransitionMode.InOut)
                    {
                        _transitionRunning = false;
                    }

                    await Transition!.AfterLeave(this);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            FirstRender = false;
        }

        if (HavingTransition)
        {
            if (_transitionJsInvoker is null)
            {
                if (Reference.Context is null)
                {
                    return;
                }

                Transition!.ElementReference = Reference;

                _transitionJsInvoker = new TransitionJsInvoker(Js);
                await _transitionJsInvoker.Init(OnTransitionEndAsync);
                await RegisterTransitionEventsAsync();
            }

            if (ElementReferenceChanged)
            {
                ElementReferenceChanged = false;

                Transition!.ElementReference = Reference;

                await RegisterTransitionEventsAsync();
            }

            await NextAsync(CurrentState);
        }
    }

    protected abstract void StartTransition();

    /// <summary>
    /// Update to the next transition state.
    /// </summary>
    /// <param name="currentState"></param>
    /// <returns></returns>
    protected abstract Task NextAsync(TransitionState currentState);

    protected virtual Task OnTransitionEndAsync(string referenceId, LeaveEnter transition) => Task.CompletedTask;

    protected async Task RequestAnimationFrameAsync(Func<Task> callback)
    {
        await Task.Delay(16);
        await callback();
    }

    private async Task RegisterTransitionEventsAsync()
    {
        if (Reference.Context is not null && _transitionJsInvoker is not null)
        {
            await _transitionJsInvoker.RegisterTransitionEvents(Reference);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_transitionJsInvoker is not null)
            {
                await _transitionJsInvoker.DisposeAsync();
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }
}