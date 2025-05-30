// JavaScript library for accessing amplitude audio from Web Audio API directly from the web browser
var AmplitudeLib = {
    /** Contains all the currently running analyzers */
    $analyzers: {},

    /** Create an analyzer and connect it to the audio source
     * @param uniqueName Analyzer name
     * @param clipDuration Clip duration
     * @param sampleSize Float array sample size
     * @returns true on success, false on failure */
    WebGL_StartSampling: function (uniqueName, clipDuration, sampleSize, dataType) {
        var errorMargin = 0.075;
        var analyzerName = UTF8ToString(uniqueName);
        var analyzer = null;
        var audioInstance = null;
        var source = null;
        var dType = UTF8ToString(dataType);

        try {
            if (typeof WEBAudio != 'undefined' && WEBAudio.audioInstanceIdCounter > 1) {
                for (var i = 1; i <= WEBAudio.audioInstanceIdCounter; i++) {
                    if (WEBAudio.audioInstances[i] != null) {
                        var pAudioInstance = WEBAudio.audioInstances[i];
                        if (pAudioInstance != null && pAudioInstance.buffer != null && 
                            Math.abs(pAudioInstance.buffer.duration - clipDuration) < errorMargin) {
                            audioInstance = pAudioInstance;
                            source = pAudioInstance.createSourceNode();
                            source.start();
                            break;
                        }
                    }
                }

                if (source == null) {
                    return false;
                }

                analyzer = WEBAudio.audioContext.createAnalyser();

                if (dType == 'Amplitude') {
                    analyzer.fftSize = sampleSize;
                } else { // Frequency
                    analyzer.fftSize = sampleSize * 2;
                }
                source.connect(analyzer);
                analyzers[analyzerName] = { analyzer: analyzer, source: source};
                return true;
            }
        }
        catch (e) {
            console.log("Failed to create or connect analyzer to source " + e);

            if (analyzer != null && source != null) {
                source.disconnect(analyzer);
            }
        }
        return false;
    },
    
    /** Delete the analyzer
     * @param uniqueName Analyzer name
     * @returns true on success, false on failure */
    WebGL_StopSampling: function (uniqueName) {
        var analyzerName = UTF8ToString(uniqueName);
        var analyzerObj = analyzers[analyzerName];

        if (analyzerObj != null) {
            try {
                analyzerObj.source.disconnect(analyzerObj);
                delete analyzers[analyzerName];
                return true;
            }
            catch (e) {
                console.log("Failed to delete analyzer (" + analyzerName + ") from source " + e);
            }
        }
        return false;
    },

    /** Fill the sample array with amplitude data
     * @param uniqueName Analyzer name
     * @param sample Float array pointer to hold amplitude data
     * @param sampleSize Float array sample size
     * @returns true on success, false on failure */
    WebGL_GetAmplitude: function (uniqueName, sample, sampleSize) {
        try {
            var analyzerName = UTF8ToString(uniqueName);
            var analyzerObj = analyzers[analyzerName];
            var buffer = new Uint8Array(Module.HEAPU8.buffer, sample, Float32Array.BYTES_PER_ELEMENT * sampleSize);
            buffer = new Float32Array(buffer.buffer, buffer.byteOffset, sampleSize);

            if (analyzerObj == null) {
                console.log("Could not find analyzer (" + analyzerName + ")");
                return false;
            }

            analyzerObj.analyzer.getFloatTimeDomainData(buffer);
            return true;
        }
        catch (e) {
            console.log("Failed to get sample data " + e);
        }
        return false;
    },

    /** Fill the sample array with frequency data
     * @param uniqueName Analyzer name
     * @param sample Float array pointer to hold amplitude data
     * @param sampleSize Float array sample size
     * @returns true on success, false on failure */
    WebGL_GetFrequency: function (uniqueName, sample) {
        try {
            var analyzerName = UTF8ToString(uniqueName);
            var analyzerObj = analyzers[analyzerName];
            var bufferLength = analyzerObj.analyzer.frequencyBinCount;
            var buffer = new Uint8Array(Module.HEAPU8.buffer, sample, Float32Array.BYTES_PER_ELEMENT * bufferLength);
            buffer = new Float32Array(buffer.buffer, buffer.byteOffset, bufferLength);
            analyzerObj.analyzer.smoothingTimeConstant = 0;

            if (analyzerObj == null) {
                console.log("Could not find analyzer (" + analyzerName + ")");
                return false;
            }

            analyzerObj.analyzer.getFloatFrequencyData(buffer);
            return true;
        }
        catch (e) {
            console.log("Failed to get sample data " + e);
        }
        return false;
    }
};

autoAddDeps(AmplitudeLib, '$analyzers');
mergeInto(LibraryManager.library, AmplitudeLib);