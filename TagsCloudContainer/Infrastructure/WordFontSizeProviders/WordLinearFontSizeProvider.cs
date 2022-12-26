﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagsCloudContainer.Infrastructure.Settings;

namespace TagsCloudContainer.Infrastructure.WordFontSizeProviders
{
    public class WordLinearFontSizeProvider : IWordFontSizeProvider
    {
        private readonly WordFontSizeSettings settings;

        private readonly int minFrequency;
        private readonly int maxFrequency;
        private readonly float frequencyDelta;

        private readonly float fontSizeDelta;

        public WordLinearFontSizeProvider(WordFontSizeSettings settings) 
        {
            this.settings = settings;
            fontSizeDelta = settings.MaxFrequencyFontSize - settings.MinFrequencyFontSize;

            minFrequency = settings.WordFrequencies.Values.Min();
            maxFrequency = settings.WordFrequencies.Values.Max();
            frequencyDelta = maxFrequency - minFrequency;
        }

        public float GetFontSize(string word)
        {
            if (!settings.WordFrequencies.ContainsKey(word))
                throw new ArgumentException($"Can't generate font size for { word }");
            return GetFontSizeWithNormalizedFrequency(GetNormalizedFrequency(settings.WordFrequencies[word]));
        }

        private float GetNormalizedFrequency(int frequency)
        {
            return (frequency - minFrequency) / frequencyDelta;
        }

        private float GetFontSizeWithNormalizedFrequency(float normalizedFrequency)
        {
            return settings.MinFrequencyFontSize + fontSizeDelta * normalizedFrequency;
        }
    }
}