﻿using BlazorComponent.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace BlazorComponent
{
    public partial class Transition : ComponentBase
    {
        private bool _firstRender = true;
        private bool _visible;
        private bool _value;
        private CancellationTokenSource _cancellationTokenSource;

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public string Name { get; set; } = "m";

        [Inject]
        public Document Document { get; set; }

        [Parameter]
        public Func<Task> OnBeforeEnter { get; set; }

        [Parameter]
        public Func<Task> OnAfterEnter { get; set; }

        [Parameter]
        public Func<Task> OnBeforeLeave { get; set; }

        [Parameter]
        public Func<Task> OnAfterLeave { get; set; }

        [Parameter]
        public Func<Task> OnEnterCancelled { get; set; }

        [Parameter]
        public Func<Task> OnLeaveCancelled { get; set; }

        internal TransitionMode Mode { get; set; }

        internal bool Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (_value == value)
                {
                    return;
                }

                _value = value;

                if (!_firstRender || Mode == TransitionMode.If)
                {
                    RunTransition();
                }
            }
        }

        protected TransitionState State { get; private set; }

        protected CssBuilder CssBuilder { get; set; } = new();

        protected StyleBuilder StyleBuilder { get; set; } = new();

        public HtmlElement Element { get; private set; }

        public string Class => CssBuilder.Class;

        public string Style => StyleBuilder.Style;

        public string Id { get; private set; }

        protected override void OnInitialized()
        {
            CssBuilder
                .AddIf(() => $"{Name}-enter {Name}-enter-active", () => State == TransitionState.Enter)
                .AddIf(() => $"{Name}-enter-active {Name}-enter-to", () => State == TransitionState.EnterTo)
                .AddIf(() => $"{Name}-leave {Name}-leave-active", () => State == TransitionState.Leave)
                .AddIf(() => $"{Name}-leave-active {Name}-leave-to", () => State == TransitionState.LeaveTo);

            StyleBuilder
                .AddIf("display:none !important", () => !_visible && Mode == TransitionMode.Show);

            Id = "_tr_" + Guid.NewGuid().ToString();

            Element = Document.QuerySelector($"[{Id}]");
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _firstRender = false;

                if (Value)
                {
                    _visible = true;
                    StateHasChanged();
                }
            }

            return Task.CompletedTask;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var sequence = 0;
            builder.OpenComponent<CascadingValue<Transition>>(sequence++);
            builder.AddAttribute(sequence++, "Value", this);

            if (Mode == TransitionMode.Show || (Mode == TransitionMode.If && _visible))
            {
                builder.AddAttribute(sequence++, "ChildContent", ChildContent);
            }

            builder.CloseComponent();
        }

        private void RunTransition()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            if (State != TransitionState.None)
            {
                State = TransitionState.None;
                StateHasChanged();
            }

            _ = Task.Run(async () =>
              {
                  var value = _value;
                  if (_value)
                  {
                      State = TransitionState.BeforeEnter;
                      if (OnBeforeEnter != null)
                      {
                          await OnBeforeEnter?.Invoke();
                      }

                      State = TransitionState.Enter;
                      await OnEnterAsync();
                      await InvokeAsync(StateHasChanged);

                      await Task.Delay(16, _cancellationTokenSource.Token);
                      _visible = true;
                      await InvokeAsync(StateHasChanged);

                      await Task.Delay(16, _cancellationTokenSource.Token);
                      State = TransitionState.EnterTo;
                      await OnEnterToAsync();
                      await InvokeAsync(StateHasChanged);

                      await Task.Delay(300, _cancellationTokenSource.Token);
                      State = TransitionState.None;
                      await InvokeAsync(StateHasChanged);

                      if (OnAfterEnter != null)
                      {
                          await OnAfterEnter?.Invoke();
                      }
                  }
                  else
                  {
                      State = TransitionState.BeforeLeave;
                      if (OnBeforeLeave != null)
                      {
                          await OnBeforeLeave?.Invoke();
                      }

                      State = TransitionState.Leave;
                      await OnLeaveAsync();
                      await InvokeAsync(StateHasChanged);

                      await Task.Delay(16, _cancellationTokenSource.Token);
                      State = TransitionState.LeaveTo;
                      await OnLeaveToAsync();
                      await InvokeAsync(StateHasChanged);

                      await Task.Delay(300, _cancellationTokenSource.Token);
                      State = TransitionState.None;
                      _visible = false;
                      await InvokeAsync(StateHasChanged);

                      if (OnAfterLeave != null)
                      {
                          await OnAfterLeave?.Invoke();
                      }
                  }

                  if (_value != value)
                  {
                      RunTransition();
                  }
              });
        }

        protected virtual Task OnEnterAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnEnterToAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnLeaveAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnLeaveToAsync()
        {
            return Task.CompletedTask;
        }
    }
}
