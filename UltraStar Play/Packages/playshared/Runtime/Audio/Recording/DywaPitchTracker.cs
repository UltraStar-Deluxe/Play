/* This code is a C# port of dywapitchtrack by Antoine Schmitt.
   It implements a wavelet algorithm, described in a paper by Eric Larson and Ross Maddox:
   “Real-Time Time-Domain Pitch Tracking Using Wavelets” of UIUC Physics.
   
   Note that the original implementation by Schmitt uses double instead of float data type.
 -------
 Dynamic Wavelet Algorithm Pitch Tracking library
 Released under the MIT open source licence
  
 Copyright (c) 2010 Antoine Schmitt
 
 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:
 
 The above copyright notice and this permission notice shall be included in
 all copies or substantial portions of the Software.
 
 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 THE SOFTWARE.
*/

public class DywaPitchTracker
{
    private float prevPitch;
    private int pitchConfidence;

    // Algorithm parameters
    public int MaxFlwtLevels { get; set; } = 6;
    public float MaxFrequency { get; set; } = 3000.0f;
    public int DifferenceLevelsN { get; set; } = 3;
    public float MaximaThresholdRatio { get; set; } = 0.75f;
    public int SampleRateHz { get; set; } = 44100;

    public DywaPitchTracker()
    {
        ClearPitchHistory();
    }

    // ************************************
    // The API main entry points
    // ************************************

    // samples : the sample buffer
    // startsample : the index of the first sample to use in the sample buffer
    // samplecount : the number of samples to use to compute the pitch
    // return : the frequency in Hz of the found pitch, or 0 if no pitch was found (sound too low, noise, etc..)
    public float ComputePitch(float[] samples, int startsample, int samplecount)
    {
        float raw_pitch = ComputeWaveletPitch(samples, startsample, samplecount);

        // Note: The algorithm currently assumes a 44100Hz audio sampling rate. If you use a different
        // samplerate, you can just multiply the resulting pitch by the ratio between your samplerate and 44100.
        // -> This ratio is stored in rawPitchScaleFactor.
        if (SampleRateHz != 44100)
        {
            raw_pitch *= (SampleRateHz / 44100f);
        }

        return DynamicPostProcessing(raw_pitch);
    }

    public void ClearPitchHistory()
    {
        prevPitch = -1.0f;
        pitchConfidence = -1;
    }

    public int NeededSampleCount(int minFreq)
    {
        int nbSam = 3 * 44100 / minFreq; // 1017. for 130 Hz
        nbSam = CeilPowerOf2(nbSam); // 1024
        return nbSam;
    }

    // ************************************
    // utility methods
    // ************************************

    // Absolute value (float)
    private float FloatAbs(float a)
    {
        return (a < 0) ? -a : a;
    }

    // Returns 1 if power of 2
    private int IsPowerOf2(int value)
    {
        if (value == 0)
        {
            return 1;
        }
        if (value == 2)
        {
            return 1;
        }
        if ((value & 0x1) != 0)
        {
            return 0;
        }
        return (IsPowerOf2(value >> 1));
    }

    // Count number of bits
    private int Bitcount(int value)
    {
        if (value == 0)
        {
            return 0;
        }
        if (value == 1)
        {
            return 1;
        }
        if (value == 2)
        {
            return 2;
        }
        return Bitcount(value >> 1) + 1;
    }

    // Closest power of 2 above or equal to the given value
    private int CeilPowerOf2(int value)
    {
        if (IsPowerOf2(value) != 0)
        {
            return value;
        }

        if (value == 1)
        {
            return 2;
        }
        int i = Bitcount(value);
        int res = 1;
        for (int j = 0; j < i; j++)
        {
            res <<= 1;
        }
        return res;
    }

    // Closest power of 2 below or equal to the given value
    private int FloorPowerOf2(int value)
    {
        if (IsPowerOf2(value) != 0)
        {
            return value;
        }
        return CeilPowerOf2(value) / 2;
    }

    // Maximum value (int)
    private int IntMax(int a, int b)
    {
        return (a > b) ? a : b;
    }

    // Minimum value (int)
    private int IntMin(int a, int b)
    {
        return (a < b) ? a : b;
    }

    // Absolute value (int)
    private int IntAbs(int x)
    {
        if (x >= 0)
        {
            return x;
        }
        return -x;
    }

    // Computes 2 to the power of n
    private int PowerOf2(int n)
    {
        int res = 1;
        for (int j = 0; j < n; j++)
        {
            res <<= 1;
        }
        return res;
    }

    //******************************
    // the Wavelet algorithm itself
    //******************************

    private float ComputeWaveletPitch(float[] samples, int startsample, int samplecount)
    {
        float pitchF = 0.0f;

        float si;
        float si1;

        // must be a power of 2
        samplecount = FloorPowerOf2(samplecount);

        // copy samples into sam
        float[] sam = new float[samplecount];
        for (int index = 0; index < samplecount; index++)
        {
            sam[index] = samples[index + startsample];
        }
        int curSamNb = samplecount;

        int[] distances = new int[samplecount];
        int[] mins = new int[samplecount];
        int[] maxs = new int[samplecount];
        int nbMins, nbMaxs;

        float ampltitudeThreshold;
        float theDC = 0.0f;

        { // compute ampltitudeThreshold and theDC
          // first compute theDC and maxAmplitude
            float maxValue = -float.MaxValue;
            float minValue = float.MaxValue;
            for (int i = 0; i < samplecount; i++)
            {
                si = sam[i];
                theDC = theDC + si;
                if (si > maxValue) maxValue = si;
                if (si < minValue) minValue = si;
            }
            theDC /= samplecount;
            maxValue -= theDC;
            minValue -= theDC;
            float amplitudeMax = (maxValue > -minValue ? maxValue : -minValue);

            ampltitudeThreshold = amplitudeMax * MaximaThresholdRatio;
            //asLog("dywapitch theDC=%f ampltitudeThreshold=%f\n", theDC, ampltitudeThreshold);
        }

        // levels, start without downsampling..
        int curLevel = 0;
        float curModeDistance = -1.0f;
        int delta;

        while (true)
        {

            // delta
            delta = (int)(44100.0f / (PowerOf2(curLevel) * MaxFrequency));
            //("dywapitch doing level=%ld delta=%ld\n", curLevel, delta);

            if (curSamNb < 2)
            {
                goto cleanup;
            }

            // compute the first maximums and minumums after zero-crossing
            // store if greater than the min threshold
            // and if at a greater distance than delta
            float dv, previousDV = -1000;
            nbMins = nbMaxs = 0;
            int lastMinIndex = -1000000;
            int lastmaxIndex = -1000000;
            int findMax = 0;
            int findMin = 0;
            for (int i = 1; i < curSamNb; i++)
            {
                si = sam[i] - theDC;
                si1 = sam[i - 1] - theDC;

                if (si1 <= 0 && si > 0) { findMax = 1; findMin = 0; }
                if (si1 >= 0 && si < 0) { findMin = 1; findMax = 0; }

                // min or max ?
                dv = si - si1;

                if (previousDV > -1000)
                {

                    if (findMin != 0 && previousDV < 0 && dv >= 0)
                    {
                        // minimum
                        if (FloatAbs(si1) >= ampltitudeThreshold)
                        {
                            if (i - 1 > lastMinIndex + delta)
                            {
                                mins[nbMins++] = i - 1;
                                lastMinIndex = i - 1;
                                findMin = 0;
                                //if DEBUGG then put "min ok"&&si
                                //
                            }
                            else
                            {
                                //if DEBUGG then put "min too close to previous"&&(i - lastMinIndex)
                                //
                            }
                        }
                        else
                        {
                            // if DEBUGG then put "min "&abs(si)&" < thresh = "&ampltitudeThreshold
                            //--
                        }
                    }

                    if (findMax != 0 && previousDV > 0 && dv <= 0)
                    {
                        // maximum
                        if (FloatAbs(si1) >= ampltitudeThreshold)
                        {
                            if (i - 1 > lastmaxIndex + delta)
                            {
                                maxs[nbMaxs++] = i - 1;
                                lastmaxIndex = i - 1;
                                findMax = 0;
                            }
                            else
                            {
                                //if DEBUGG then put "max too close to previous"&&(i - lastmaxIndex)
                                //--
                            }
                        }
                        else
                        {
                            //if DEBUGG then put "max "&abs(si)&" < thresh = "&ampltitudeThreshold
                            //--
                        }
                    }
                }

                previousDV = dv;
            }

            if (nbMins == 0 && nbMaxs == 0)
            {
                // no best distance !
                //asLog("dywapitch no mins nor maxs, exiting\n");

                // if DEBUGG then put "no mins nor maxs, exiting"
                goto cleanup;
            }
            //if DEBUGG then put count(maxs)&&"maxs &"&&count(mins)&&"mins"

            // maxs = [5, 20, 100,...]
            // compute distances
            int d;
            for (int index = 0; index < distances.Length; index++)
            {
                distances[index] = 0;
            }

            for (int i = 0; i < nbMins; i++)
            {
                for (int j = 1; j < DifferenceLevelsN; j++)
                {
                    if (i + j < nbMins)
                    {
                        d = IntAbs(mins[i] - mins[i + j]);
                        //asLog("dywapitch i=%ld j=%ld d=%ld\n", i, j, d);
                        distances[d] = distances[d] + 1;
                    }
                }
            }
            for (int i = 0; i < nbMaxs; i++)
            {
                for (int j = 1; j < DifferenceLevelsN; j++)
                {
                    if (i + j < nbMaxs)
                    {
                        d = IntAbs(maxs[i] - maxs[i + j]);
                        //asLog("dywapitch i=%ld j=%ld d=%ld\n", i, j, d);
                        distances[d] = distances[d] + 1;
                    }
                }
            }

            // find best summed distance
            int bestDistance = -1;
            int bestValue = -1;
            for (int i = 0; i < curSamNb; i++)
            {
                int summed = 0;
                for (int j = -delta; j <= delta; j++)
                {
                    if (i + j >= 0 && i + j < curSamNb)
                    {
                        summed += distances[i + j];
                    }
                }
                //asLog("dywapitch i=%ld summed=%ld bestDistance=%ld\n", i, summed, bestDistance);
                if (summed == bestValue)
                {
                    if (i == 2 * bestDistance)
                    {
                        bestDistance = i;
                    }
                }
                else if (summed > bestValue)
                {
                    bestValue = summed;
                    bestDistance = i;
                }
            }
            //asLog("dywapitch bestDistance=%ld\n", bestDistance);

            // averaging
            float distAvg = 0.0f;
            float nbDists = 0;
            for (int j = -delta; j <= delta; j++)
            {
                if (bestDistance + j >= 0 && bestDistance + j < samplecount)
                {
                    int nbDist = distances[bestDistance + j];
                    if (nbDist > 0)
                    {
                        nbDists += nbDist;
                        distAvg += (bestDistance + j) * nbDist;
                    }
                }
            }
            // this is our mode distance !
            distAvg /= nbDists;
            //asLog("dywapitch distAvg=%f\n", distAvg);

            // continue the levels ?
            if (curModeDistance > -1.0f)
            {
                float similarity = FloatAbs(distAvg * 2 - curModeDistance);
                if (similarity <= 2 * delta)
                {
                    //if DEBUGG then put "similarity="&similarity&&"delta="&delta&&"ok"
                    //asLog("dywapitch similarity=%f OK !\n", similarity);
                    // two consecutive similar mode distances : ok !
                    pitchF = 44100.0f / (PowerOf2(curLevel - 1) * curModeDistance);
                    goto cleanup;
                }
                //if DEBUGG then put "similarity="&similarity&&"delta="&delta&&"not"
            }

            // not similar, continue next level
            curModeDistance = distAvg;

            curLevel += 1;
            if (curLevel >= MaxFlwtLevels)
            {
                // put "max levels reached, exiting"
                //asLog("dywapitch max levels reached, exiting\n");
                goto cleanup;
            }

            // downsample
            if (curSamNb < 2)
            {
                //asLog("dywapitch not enough samples, exiting\n");
                goto cleanup;
            }
            for (int i = 0; i < curSamNb / 2; i++)
            {
                sam[i] = (sam[2 * i] + sam[2 * i + 1]) / 2.0f;
            }
            curSamNb /= 2;
        }

cleanup:
// No need to free stuff in C#

        return pitchF;
    }

    // ***********************************
    // the dynamic post-processing
    // ***********************************

    /***
    It states: 
     - a pitch cannot change much all of a sudden (20%) (impossible humanly,
     so if such a situation happens, consider that it is a mistake and drop it. 
     - a pitch cannot float or be divided by 2 all of a sudden : it is an
     algorithm side-effect : divide it or float it by 2. 
     - a lonely voiced pitch cannot happen, nor can a sudden drop in the middle
     of a voiced segment. Smooth the plot. 
    ***/

    private float DynamicPostProcessing(float pitch)
    {

        // equivalence
        if (pitch == 0.0f)
        {
            pitch = -1.0f;
        }

        float estimatedPitch = -1;
        float acceptedError = 0.2f;
        int maxConfidence = 5;

        if (pitch != -1)
        {
            // I have a pitch here
            if (prevPitch == -1)
            {
                // no previous
                estimatedPitch = pitch;
                prevPitch = pitch;
                pitchConfidence = 1;

            }
            else if (FloatAbs(prevPitch - pitch) / pitch < acceptedError)
            {
                // similar : remember and increment pitch
                prevPitch = pitch;
                estimatedPitch = pitch;
                pitchConfidence = IntMin(maxConfidence, pitchConfidence + 1); // maximum 3
            }
            else if ((pitchConfidence >= maxConfidence - 2) && FloatAbs(prevPitch - 2.0f * pitch) / (2.0f * pitch) < acceptedError)
            {
                // close to half the last pitch, which is trusted
                estimatedPitch = 2.0f * pitch;
                prevPitch = estimatedPitch;

            }
            else if ((pitchConfidence >= maxConfidence - 2) && FloatAbs(prevPitch - 0.5f * pitch) / (0.5f * pitch) < acceptedError)
            {
                // close to twice the last pitch, which is trusted
                estimatedPitch = 0.5f * pitch;
                prevPitch = estimatedPitch;

            }
            else
            {
                // nothing like this : very different value
                if (pitchConfidence >= 1)
                {
                    // previous trusted : keep previous
                    estimatedPitch = prevPitch;
                    pitchConfidence = IntMax(0, pitchConfidence - 1);
                }
                else
                {
                    // previous not trusted : take current
                    estimatedPitch = pitch;
                    prevPitch = pitch;
                    pitchConfidence = 1;
                }
            }

        }
        else
        {
            // no pitch now
            if (prevPitch != -1)
            {
                // was pitch before
                if (pitchConfidence >= 1)
                {
                    // continue previous
                    estimatedPitch = prevPitch;
                    pitchConfidence = IntMax(0, pitchConfidence - 1);
                }
                else
                {
                    prevPitch = -1;
                    estimatedPitch = -1.0f;
                    pitchConfidence = 0;
                }
            }
        }

        // put "_pitchConfidence="&_pitchConfidence
        if (pitchConfidence >= 1)
        {
            // ok
            pitch = estimatedPitch;
        }
        else
        {
            pitch = -1;
        }

        // equivalence
        if (pitch == -1)
        {
            pitch = 0.0f;
        }

        return pitch;
    }
}
