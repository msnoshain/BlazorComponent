﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorComponent.Components.Core.CssProcess
{
    public class BuilderBase
    {
        internal readonly Dictionary<Func<string>, Func<bool>> _mapper = new();

        public BuilderContext Context { get; internal set; }
    }
}