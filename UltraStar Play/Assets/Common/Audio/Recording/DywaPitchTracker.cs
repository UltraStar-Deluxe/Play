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
    private float _prevPitch;
    private int _pitchConfidence;

    // algorithm parameters
    public int maxFLWTlevels { get; set; } = 6;
    public float maxFrequency { get; set; } = 3000.0f;
    public int differenceLevelsN { get; set; } = 3;
    public float maximaThresholdRatio { get; set; } = 0.75f;
    public int sampleRateHz { get; set; } = 44100;

    public DywaPitchTracker()
    {
        _prevPitch = -1.0f;
        _pitchConfidence = -1;
    }

    // absolute value (float)
    private float fabs(float a)
    {
        return (a < 0) ? -a : a;
    }

    // returns 1 if power of 2
    private int _power2p(int value)
    {
        if (value == 0) return 1;
        if (value == 2) return 1;
        if ((value & 0x1) != 0) return 0;
        return (_power2p(value >> 1));
    }

    // count number of bits
    private int _bitcount(int value)
    {
        if (value == 0) return 0;
        if (value == 1) return 1;
        if (value == 2) return 2;
        return _bitcount(value >> 1) + 1;
    }

    // closest power of 2 above or equal
    private int _ceil_power2(int value)
    {
        if (_power2p(value) != 0) return value;

        if (value == 1) return 2;
        int j, i = _bitcount(value);
        int res = 1;
        for (j = 0; j < i; j++) res <<= 1;
        return res;
    }

    // closest power of 2 below or equal
    private int _floor_power2(int value)
    {
        if (_power2p(value) != 0) return value;
        return _ceil_power2(value) / 2;
    }

    // maximum value (int)
    private int imax(int a, int b)
    {
        return (a > b) ? a : b;
    }

    // minimum value (int)
    private int imin(int a, int b)
    {
        return (a < b) ? a : b;
    }

    // absolute value (int)
    private int _iabs(int x)
    {
        if (x >= 0) return x;
        return -x;
    }

    // 2 power
    private int _2power(int i)
    {
        int res = 1, j;
        for (j = 0; j < i; j++) res <<= 1;
        return res;
    }

    //******************************
    // the Wavelet algorithm itself
    //******************************

    public int dywapitch_neededsamplecount(int minFreq)
    {
        int nbSam = 3 * 44100 / minFreq; // 1017. for 130 Hz
        nbSam = _ceil_power2(nbSam); // 1024
        return nbSam;
    }

    private float _dywapitch_computeWaveletPitch(float[] samples, int startsample, int samplecount)
    {
        float pitchF = 0.0f;

        int i, j;
        float si, si1;

        // must be a power of 2
        samplecount = _floor_power2(samplecount);

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
            for (i = 0; i < samplecount; i++)
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

            ampltitudeThreshold = amplitudeMax * maximaThresholdRatio;
            //asLog("dywapitch theDC=%f ampltitudeThreshold=%f\n", theDC, ampltitudeThreshold);
        }

        // levels, start without downsampling..
        int curLevel = 0;
        float curModeDistance = -1.0f;
        int delta;

        while (true)
        {

            // delta
            delta = (int)(44100.0f / (_2power(curLevel) * maxFrequency));
            //("dywapitch doing level=%ld delta=%ld\n", curLevel, delta);

            if (curSamNb < 2) goto cleanup;

            // compute the first maximums and minumums after zero-crossing
            // store if greater than the min threshold
            // and if at a greater distance than delta
            float dv, previousDV = -1000;
            nbMins = nbMaxs = 0;
            int lastMinIndex = -1000000;
            int lastmaxIndex = -1000000;
            int findMax = 0;
            int findMin = 0;
            for (i = 1; i < curSamNb; i++)
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
                        if (fabs(si1) >= ampltitudeThreshold)
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
                        if (fabs(si1) >= ampltitudeThreshold)
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

            for (i = 0; i < nbMins; i++)
            {
                for (j = 1; j < differenceLevelsN; j++)
                {
                    if (i + j < nbMins)
                    {
                        d = _iabs(mins[i] - mins[i + j]);
                        //asLog("dywapitch i=%ld j=%ld d=%ld\n", i, j, d);
                        distances[d] = distances[d] + 1;
                    }
                }
            }
            for (i = 0; i < nbMaxs; i++)
            {
                for (j = 1; j < differenceLevelsN; j++)
                {
                    if (i + j < nbMaxs)
                    {
                        d = _iabs(maxs[i] - maxs[i + j]);
                        //asLog("dywapitch i=%ld j=%ld d=%ld\n", i, j, d);
                        distances[d] = distances[d] + 1;
                    }
                }
            }

            // find best summed distance
            int bestDistance = -1;
            int bestValue = -1;
            for (i = 0; i < curSamNb; i++)
            {
                int summed = 0;
                for (j = -delta; j <= delta; j++)
                {
                    if (i + j >= 0 && i + j < curSamNb)
                        summed += distances[i + j];
                }
                //asLog("dywapitch i=%ld summed=%ld bestDistance=%ld\n", i, summed, bestDistance);
                if (summed == bestValue)
                {
                    if (i == 2 * bestDistance)
                        bestDistance = i;

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
            for (j = -delta; j <= delta; j++)
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
                float similarity = fabs(distAvg * 2 - curModeDistance);
                if (similarity <= 2 * delta)
                {
                    //if DEBUGG then put "similarity="&similarity&&"delta="&delta&&"ok"
                    //asLog("dywapitch similarity=%f OK !\n", similarity);
                    // two consecutive similar mode distances : ok !
                    pitchF = 44100.0f / (_2power(curLevel - 1) * curModeDistance);
                    goto cleanup;
                }
                //if DEBUGG then put "similarity="&similarity&&"delta="&delta&&"not"
            }

            // not similar, continue next level
            curModeDistance = distAvg;

            curLevel += 1;
            if (curLevel >= maxFLWTlevels)
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
            for (i = 0; i < curSamNb / 2; i++)
            {
                sam[i] = (sam[2 * i] + sam[2 * i + 1]) / 2.0f;
            }
            curSamNb /= 2;
        }

///
cleanup:
// No need to free stuff in C#
//free(distances);
//free(mins);
//free(maxs);
//free(sam);

        return pitchF;
    }

    // ***********************************
    // the dynamic postprocess
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

    private float _dywapitch_dynamicprocess(float pitch)
    {

        // equivalence
        if (pitch == 0.0f) pitch = -1.0f;

        //
        float estimatedPitch = -1;
        float acceptedError = 0.2f;
        int maxConfidence = 5;

        if (pitch != -1)
        {
            // I have a pitch here

            if (_prevPitch == -1)
            {
                // no previous
                estimatedPitch = pitch;
                _prevPitch = pitch;
                _pitchConfidence = 1;

            }
            else if (fabs(_prevPitch - pitch) / pitch < acceptedError)
            {
                // similar : remember and increment pitch
                _prevPitch = pitch;
                estimatedPitch = pitch;
                _pitchConfidence = imin(maxConfidence, _pitchConfidence + 1); // maximum 3
            }
            else if ((_pitchConfidence >= maxConfidence - 2) && fabs(_prevPitch - 2.0f * pitch) / (2.0f * pitch) < acceptedError)
            {
                // close to half the last pitch, which is trusted
                estimatedPitch = 2.0f * pitch;
                _prevPitch = estimatedPitch;

            }
            else if ((_pitchConfidence >= maxConfidence - 2) && fabs(_prevPitch - 0.5f * pitch) / (0.5f * pitch) < acceptedError)
            {
                // close to twice the last pitch, which is trusted
                estimatedPitch = 0.5f * pitch;
                _prevPitch = estimatedPitch;

            }
            else
            {
                // nothing like this : very different value
                if (_pitchConfidence >= 1)
                {
                    // previous trusted : keep previous
                    estimatedPitch = _prevPitch;
                    _pitchConfidence = imax(0, _pitchConfidence - 1);
                }
                else
                {
                    // previous not trusted : take current
                    estimatedPitch = pitch;
                    _prevPitch = pitch;
                    _pitchConfidence = 1;
                }
            }

        }
        else
        {
            // no pitch now
            if (_prevPitch != -1)
            {
                // was pitch before
                if (_pitchConfidence >= 1)
                {
                    // continue previous
                    estimatedPitch = _prevPitch;
                    _pitchConfidence = imax(0, _pitchConfidence - 1);
                }
                else
                {
                    _prevPitch = -1;
                    estimatedPitch = -1.0f;
                    _pitchConfidence = 0;
                }
            }
        }

        // put "_pitchConfidence="&_pitchConfidence
        if (_pitchConfidence >= 1)
        {
            // ok
            pitch = estimatedPitch;
        }
        else
        {
            pitch = -1;
        }

        // equivalence
        if (pitch == -1) pitch = 0.0f;

        return pitch;
    }

    // ************************************
    // the API main entry points
    // ************************************

    // samples : the sample buffer
    // startsample : the index of the first sample to use in the sample buffer
    // samplecount : the number of samples to use to compute the pitch
    // return : the frequency in Hz of the found pitch, or 0 if no pitch was found (sound too low, noise, etc..)
    public float ComputePitch(float[] samples, int startsample, int samplecount)
    {
        float raw_pitch = _dywapitch_computeWaveletPitch(samples, startsample, samplecount);

        // Note: The algorithm currently assumes a 44100Hz audio sampling rate. If you use a different
        // samplerate, you can just multiply the resulting pitch by the ratio between your samplerate and 44100.
        // -> This ratio is stored in rawPitchScaleFactor.
        if (sampleRateHz != 44100)
        {
            raw_pitch *= (sampleRateHz / 44100f);
        }

        return raw_pitch;
        //return _dywapitch_dynamicprocess(raw_pitch);
    }
}