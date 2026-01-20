var WebGLAudioSpectrum = {

    $AudioSpectrumState: {
        analyser: null,
        dataArray: null,
        isInitialized: false,
        fftSize: 1024
    },

    WebGLAudioSpectrum_Initialize: function(fftSize) {
        // Unity's WebGL audio uses a global AudioContext
        // We need to find and hook into it
        var audioContext = null;

        // Try to find Unity's audio context
        if (typeof WEBAudio !== 'undefined' && WEBAudio.audioContext) {
            audioContext = WEBAudio.audioContext;
        } else if (typeof window.unityAudioContext !== 'undefined') {
            audioContext = window.unityAudioContext;
        } else {
            // Fallback: look for any AudioContext on the page
            var contexts = ['audioContext', 'webkitAudioContext'];
            for (var i = 0; i < contexts.length; i++) {
                if (window[contexts[i]]) {
                    audioContext = window[contexts[i]];
                    break;
                }
            }
        }

        if (!audioContext) {
            console.warn('[WebGLAudioSpectrum] Could not find AudioContext. Spectrum analysis unavailable.');
            return false;
        }

        try {
            // Create analyser node
            AudioSpectrumState.analyser = audioContext.createAnalyser();
            AudioSpectrumState.analyser.fftSize = fftSize;
            AudioSpectrumState.analyser.smoothingTimeConstant = 0.8;
            AudioSpectrumState.fftSize = fftSize;

            // Create data array for frequency data
            var bufferLength = AudioSpectrumState.analyser.frequencyBinCount;
            AudioSpectrumState.dataArray = new Float32Array(bufferLength);

            // Try to connect to Unity's audio destination
            // Unity WebGL routes audio through WEBAudio.audioInstances
            if (typeof WEBAudio !== 'undefined' && WEBAudio.audioInstances) {
                // Hook into existing audio instances
                for (var id in WEBAudio.audioInstances) {
                    var instance = WEBAudio.audioInstances[id];
                    if (instance && instance.gain) {
                        try {
                            instance.gain.connect(AudioSpectrumState.analyser);
                            AudioSpectrumState.analyser.connect(audioContext.destination);
                            console.log('[WebGLAudioSpectrum] Connected to audio instance: ' + id);
                        } catch (e) {
                            // May already be connected or invalid
                        }
                    }
                }

                // Override the audio instance creation to hook new sources
                var originalCreateChannel = WEBAudio.audioInstances ? null : null;
                if (WEBAudio.createChannel) {
                    originalCreateChannel = WEBAudio.createChannel;
                    WEBAudio.createChannel = function(callback, error) {
                        var result = originalCreateChannel(callback, error);
                        // Try to connect new channels
                        setTimeout(function() {
                            WebGLAudioSpectrum_TryConnectNewSources();
                        }, 100);
                        return result;
                    };
                }
            }

            AudioSpectrumState.isInitialized = true;
            console.log('[WebGLAudioSpectrum] Initialized successfully. FFT size: ' + fftSize);
            return true;

        } catch (e) {
            console.error('[WebGLAudioSpectrum] Initialization failed: ' + e.message);
            return false;
        }
    },

    WebGLAudioSpectrum_TryConnectNewSources: function() {
        if (!AudioSpectrumState.isInitialized || !AudioSpectrumState.analyser) return;

        var audioContext = null;
        if (typeof WEBAudio !== 'undefined' && WEBAudio.audioContext) {
            audioContext = WEBAudio.audioContext;
        }
        if (!audioContext) return;

        if (typeof WEBAudio !== 'undefined' && WEBAudio.audioInstances) {
            for (var id in WEBAudio.audioInstances) {
                var instance = WEBAudio.audioInstances[id];
                if (instance && instance.gain && !instance._spectrumConnected) {
                    try {
                        instance.gain.disconnect();
                        instance.gain.connect(AudioSpectrumState.analyser);
                        instance._spectrumConnected = true;
                        console.log('[WebGLAudioSpectrum] Connected new audio instance: ' + id);
                    } catch (e) {
                        // Ignore connection errors
                    }
                }
            }

            // Ensure analyser is connected to destination
            try {
                AudioSpectrumState.analyser.connect(audioContext.destination);
            } catch (e) {
                // May already be connected
            }
        }
    },

    WebGLAudioSpectrum_GetSpectrumData: function(dataPtr, dataLength) {
        if (!AudioSpectrumState.isInitialized || !AudioSpectrumState.analyser) {
            // Return zeros
            for (var i = 0; i < dataLength; i++) {
                HEAPF32[(dataPtr >> 2) + i] = 0;
            }
            return;
        }

        // Try to connect any new sources that may have been created
        _WebGLAudioSpectrum_TryConnectNewSources();

        // Get frequency data (in decibels, range -100 to 0)
        AudioSpectrumState.analyser.getFloatFrequencyData(AudioSpectrumState.dataArray);

        // Convert dB to linear scale (0 to 1) similar to Unity's GetSpectrumData
        var length = Math.min(dataLength, AudioSpectrumState.dataArray.length);
        for (var i = 0; i < length; i++) {
            // Convert from dB (-100 to 0) to linear (0 to 1)
            // Unity's GetSpectrumData returns values typically in range 0 to ~0.1 for normal audio
            var db = AudioSpectrumState.dataArray[i];
            var linear = Math.pow(10, db / 20); // Convert dB to linear
            linear = Math.max(0, Math.min(1, linear)); // Clamp to 0-1
            HEAPF32[(dataPtr >> 2) + i] = linear * 0.5; // Scale down to match Unity's typical range
        }

        // Fill remaining with zeros if Unity requested more data
        for (var j = length; j < dataLength; j++) {
            HEAPF32[(dataPtr >> 2) + j] = 0;
        }
    },

    WebGLAudioSpectrum_IsInitialized: function() {
        return AudioSpectrumState.isInitialized ? 1 : 0;
    },

    WebGLAudioSpectrum_Reconnect: function() {
        // Force reconnection attempt
        _WebGLAudioSpectrum_TryConnectNewSources();
    }
};

autoAddDeps(WebGLAudioSpectrum, '$AudioSpectrumState');
mergeInto(LibraryManager.library, WebGLAudioSpectrum);
