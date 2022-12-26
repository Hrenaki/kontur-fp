﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagsCloudContainer.Infrastructure.Settings;

namespace TagsCloudContainer.Infrastructure.WordColorProviders.Factories
{
    public class WordConstColorProviderFactory : IWordColorProviderFactory
    {
        public IWordColorProvider CreateDefault(WordColorSettings settings) => new WordConstColorProvider();
    }
}