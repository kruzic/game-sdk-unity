var KruzicSDKPlugin = {
    $KruzicState: {
        initialized: false,
        unityInstance: null,

        getUnityInstance: function() {
            if (this.unityInstance) return this.unityInstance;

            // Try different Unity instance locations
            if (typeof Module !== 'undefined' && Module.SendMessage) {
                this.unityInstance = Module;
                return Module;
            }
            if (typeof unityInstance !== 'undefined' && unityInstance.SendMessage) {
                this.unityInstance = unityInstance;
                return unityInstance;
            }
            if (typeof gameInstance !== 'undefined' && gameInstance.SendMessage) {
                this.unityInstance = gameInstance;
                return gameInstance;
            }
            if (window.unityInstance && window.unityInstance.SendMessage) {
                this.unityInstance = window.unityInstance;
                return window.unityInstance;
            }

            console.warn('[Kruzic SDK] Unity instance not found');
            return null;
        },

        init: function() {
            if (this.initialized) return;
            this.initialized = true;

            window.addEventListener('message', function(event) {
                var data = event.data;
                if (!data || data.type !== 'RESPONSE') return;

                var unity = KruzicState.getUnityInstance();
                if (!unity) {
                    console.error('[Kruzic SDK] Cannot forward response - Unity instance not found');
                    return;
                }

                try {
                    // Unity expects data field as JSON string, not object
                    var responseForUnity = {
                        type: data.type,
                        requestId: data.requestId,
                        success: data.success,
                        data: data.data ? JSON.stringify(data.data) : null,
                        error: data.error
                    };
                    var responseJson = JSON.stringify(responseForUnity);
                    unity.SendMessage('KruzicSDK', 'OnMessageReceived', responseJson);
                } catch (e) {
                    console.error('[Kruzic SDK] Error forwarding response:', e);
                }
            });
        }
    },

    KruzicSendMessage: function(typePtr, requestId, payloadPtr) {
        KruzicState.init();

        var type = UTF8ToString(typePtr);
        var payloadStr = UTF8ToString(payloadPtr);
        var payload = null;

        if (payloadStr && payloadStr.length > 0) {
            try {
                payload = JSON.parse(payloadStr);
            } catch (e) {
                console.error('[Kruzic SDK] Failed to parse payload:', e);
            }
        }

        var message = {
            type: type,
            requestId: requestId,
            payload: payload
        };

        try {
            window.parent.postMessage(message, '*');
        } catch (e) {
            console.error('[Kruzic SDK] Failed to send message:', e);
        }
    },

    KruzicNotifyReady: function() {
        KruzicState.init();
        KruzicState.unityInstance = KruzicState.getUnityInstance();

        var message = {
            type: 'GAME_READY',
            requestId: 0
        };

        try {
            window.parent.postMessage(message, '*');
        } catch (e) {
            console.error('[Kruzic SDK] Failed to send ready message:', e);
        }
    },

    KruzicIsInIframe: function() {
        try {
            return window.self !== window.top;
        } catch (e) {
            return true;
        }
    }
};

autoAddDeps(KruzicSDKPlugin, '$KruzicState');
mergeInto(LibraryManager.library, KruzicSDKPlugin);
