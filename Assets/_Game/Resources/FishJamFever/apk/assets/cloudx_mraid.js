/*
 * CloudX MRAID runtime template.
 *
 * This file is loaded by MraidJavaScript.kt and rendered before WebView injection.
 * Double-braced tokens are placeholders and must not be left unresolved.
 */
(function() {

// MRAID 3.0 Environment object. SDK/privacy values are supplied by native code.
window.MRAID_ENV = {
  version: '3.0',
  sdk: 'CloudX',
  sdkVersion: '{{SDK_VERSION}}',
  appId: '{{APP_ID}}',
  ifa: '{{IFA}}',
  limitAdTracking: {{LIMIT_AD_TRACKING}}
};

window.__CLX_RENDERER_DEBUG__ = {{RENDERER_DEBUG}};

var __mraidConsoleLog = console.log.bind(console);
function mraidLog(level, message) {
  if (!window.__CLX_RENDERER_DEBUG__) return;
  __mraidConsoleLog('[CloudX MRAID] [' + level + '] ' + message);
}

if (window.__CLX_RENDERER_DEBUG__) {
  (function() {
    var originalLog = console.log;
    var originalWarn = console.warn;
    var originalError = console.error;
    var originalInfo = console.info;
    var originalDebug = console.debug;

    function forwardToNative(level, args) {
      try {
        var message = Array.prototype.slice.call(args).map(function(arg) {
          if (typeof arg === 'object') {
            try { return JSON.stringify(arg); } catch(e) { return String(arg); }
          }
          return String(arg);
        }).join(' ');
        mraidLog(level, message);
      } catch (e) { /* Ignore */ }
    }

    console.log = function() { forwardToNative('debug', arguments); originalLog.apply(console, arguments); };
    console.warn = function() { forwardToNative('warn', arguments); originalWarn.apply(console, arguments); };
    console.error = function() { forwardToNative('error', arguments); originalError.apply(console, arguments); };
    console.info = function() { forwardToNative('info', arguments); originalInfo.apply(console, arguments); };
    console.debug = function() { forwardToNative('verbose', arguments); originalDebug.apply(console, arguments); };
  })();
}

function postMessageToNative(message) {
  if (window.{{BRIDGE_NAME}}) {
    window.{{BRIDGE_NAME}}.postMessage(JSON.stringify(message));
  } else if (window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.mraid) {
    window.webkit.messageHandlers.mraid.postMessage(message);
  } else {
    console.error('[CloudX MRAID] No native bridge available for action: ' + message.action);
  }
}

/*
 * Observe-only ping for MRAID features CloudX rejects and does not support (expand / resize /
 * useCustomClose). Fire-and-forget: bypasses the native call queue (like reportContentError) so it
 * never occupies the single in-flight slot or needs nativeCallComplete. The creative has already
 * been rejected via mraid.onError(...) at the call site; native only records a counter.
 */
function reportUnsupportedFeature(feature) {
  postMessageToNative({ action: 'unsupportedFeature', feature: feature });
}

var mraid = (function() {
  'use strict';

  // ============================================
  // STATE VARIABLES
  // ============================================
  var state = 'loading';
  var placementType = '{{PLACEMENT_TYPE}}';
  var isViewable = false;
  var volumePercentage = null;
  var resizePropertiesInitialized = false;

  // Orientation properties
  var orientationProperties = {
    allowOrientationChange: true,
    forceOrientation: 'none'
  };

  // Expand properties
  var expandProperties = {
    width: {{SCREEN_WIDTH}},
    height: {{SCREEN_HEIGHT}},
    useCustomClose: false,
    isModal: true
  };

  // Resize properties
  var resizeProperties = {
    width: {{SCREEN_WIDTH}},
    height: {{SCREEN_HEIGHT}},
    customClosePosition: 'top-right',
    offsetX: 0,
    offsetY: 0,
    allowOffscreen: true
  };

  /*
   * Initial position values use screen-size placeholders so the MRAID object is usable during
   * bootstrap. Native updates defaultPosition/currentPosition with the real ad-slot geometry
   * after the WebView is attached and laid out.
   */
  var defaultPosition = { x: 0, y: 0, width: {{SCREEN_WIDTH}}, height: {{SCREEN_HEIGHT}} };
  var currentPosition = { x: 0, y: 0, width: {{SCREEN_WIDTH}}, height: {{SCREEN_HEIGHT}} };
  var maxSize = { width: {{MAX_WIDTH}}, height: {{MAX_HEIGHT}} };
  var screenSize = { width: {{SCREEN_WIDTH}}, height: {{SCREEN_HEIGHT}} };

  // Current app orientation (MRAID 3.0)
  var currentAppOrientation = {
    orientation: 'portrait',
    locked: false
  };

  // Exposure tracking (MRAID 3.0)
  var lastExposure = {
    exposedPercentage: 0.0,
    visibleRectangle: { x: 0, y: 0, width: 0, height: 0 },
    occlusionRectangles: null
  };

  // Supported features
  var allSupports = {
    sms: false,
    tel: false,
    calendar: false,
    storePicture: false,
    inlineVideo: {{SUPPORTS_INLINE_VIDEO}},
    location: false,
    vpaid: false
  };

  // Event listeners
  var eventListeners = {};
  var readyFired = false;

  // ============================================
  // NATIVE CALL QUEUE
  // Ensures ordered command execution
  // ============================================
  var nativeCallQueue = [];
  var nativeCallInProcess = false;

  function callNative(action, data) {
    var message = Object.assign({ action: action }, data || {});
    if (nativeCallInProcess) {
      nativeCallQueue.push(message);
    } else {
      nativeCallInProcess = true;
      postMessageToNative(message);
    }
  }

  // ============================================
  // EVENT SYSTEM
  // ============================================
  function fireEvent(event, args) {
    var handlers = eventListeners[event];
    if (!handlers) return;

    for (var i = 0; i < handlers.length; i++) {
      try {
        if (event === 'ready') {
          handlers[i]();
        } else if (event === 'error') {
          handlers[i](args[0], args[1]);
        } else if (event === 'stateChange' || event === 'viewableChange') {
          handlers[i](args);
        } else if (event === 'sizeChange') {
          handlers[i](args[0], args[1]);
        } else if (event === 'exposureChange') {
          handlers[i](lastExposure.exposedPercentage, lastExposure.visibleRectangle, lastExposure.occlusionRectangles);
        } else if (event === 'audioVolumeChange') {
          handlers[i](args);
        }
      } catch(e) {
        console.log('MRAID event handler error:', e);
      }
    }
  }

  function safeURLDescription(url) {
    try {
      var parsed = new URL(String(url), document.location.href);
      return parsed.host ? parsed.protocol + '//' + parsed.host : parsed.protocol.replace(':', '');
    } catch (e) {
      var value = String(url || '');
      var match = value.match(/^([a-zA-Z][a-zA-Z0-9+.-]*):/);
      return match ? match[1].toLowerCase() : '(no scheme)';
    }
  }

  // ============================================
  // INTERNAL STATE HANDLERS (called by native)
  // ============================================
  function onReady() {
    if (readyFired) return;
    readyFired = true;
    onStateChange('default');
    fireEvent('ready');
  }

  /*
   * Left in place for compatibility with the bundled MRAID JS contract even though CloudX
   * does not currently support expand. Keeping the expanded state string available here avoids
   * breaking creatives that expect the standard MRAID API surface.
   */
  function onReadyExpanded() {
    if (readyFired) return;
    readyFired = true;
    onStateChange('expanded');
    fireEvent('ready');
  }

  function onStateChange(newState) {
    if (state !== newState) {
      state = newState;
      fireEvent('stateChange', state);
    }
  }

  function onViewableChange(viewable) {
    isViewable = viewable;
    fireEvent('viewableChange', isViewable);
  }

  function onSizeChange(width, height) {
    fireEvent('sizeChange', [width, height]);
  }

  function onExposureChange(exposureJSON) {
    var exposure = typeof exposureJSON === 'string' ? JSON.parse(exposureJSON) : exposureJSON;
    lastExposure = exposure;
    fireEvent('exposureChange');
  }

  function onAudioVolumeChange(newVolumePercentage) {
    volumePercentage = newVolumePercentage;
    fireEvent('audioVolumeChange', newVolumePercentage);
  }

  function onError(message, action) {
    fireEvent('error', [message, action]);
  }

  // ============================================
  // PUBLIC MRAID API
  // ============================================
  return {
    getVersion: function() { mraidLog('debug', 'getVersion: 3.0'); return '3.0'; },
    getState: function() { mraidLog('debug', 'getState: ' + state); return state; },
    getPlacementType: function() { mraidLog('debug', 'getPlacementType: ' + placementType); return placementType; },
    isViewable: function() { mraidLog('debug', 'isViewable: ' + isViewable); return isViewable; },

    // ============================================
    // EVENT LISTENERS
    // ============================================
    addEventListener: function(event, listener) {
      var handlers = eventListeners[event];
      if (!handlers) {
        handlers = eventListeners[event] = [];
      }
      for (var i = 0; i < handlers.length; i++) {
        if (listener === handlers[i]) return;
      }
      handlers.push(listener);

      if (event === 'exposureChange') {
        setTimeout(function() { fireEvent('exposureChange'); }, 0);
      }
      if (event === 'audioVolumeChange') {
        setTimeout(function() { fireEvent('audioVolumeChange', volumePercentage); }, 0);
      }
    },

    removeEventListener: function(event, listener) {
      var handlers = eventListeners[event];
      if (handlers) {
        for (var i = 0; i < handlers.length; i++) {
          if (handlers[i] === listener) {
            handlers.splice(i, 1);
            break;
          }
        }
      }
    },

    // ============================================
    // ACTIONS
    // ============================================
    open: function(url) {
      mraidLog('debug', 'open: ' + safeURLDescription(url));
      callNative('open', { url: url });
    },

    close: function() {
      mraidLog('debug', 'close');
      callNative('close');
    },

    /*
     * Kept on the API surface for spec compatibility, but CloudX does not support expand:
     * reject directly via mraid.onError(...) so creatives degrade cleanly.
     */
    expand: function(url) {
      mraidLog('debug', 'expand: ' + (url ? safeURLDescription(url) : '(no URL)'));
      onError('Expand is not supported', 'expand');
      reportUnsupportedFeature('expand');
    },

    /*
     * Kept on the API surface for spec compatibility, but CloudX does not support resize:
     * reject directly via mraid.onError(...) so creatives degrade cleanly.
     */
    resize: function() {
      mraidLog('debug', 'resize: ' + JSON.stringify(resizeProperties));
      onError('Resize is not supported', 'resize');
      reportUnsupportedFeature('resize');
    },

    playVideo: function(url) {
      mraidLog('debug', 'playVideo: ' + safeURLDescription(url));
      onError('Play video is not supported', 'playVideo');
    },

    storePicture: function(url) {
      mraidLog('debug', 'storePicture: ' + safeURLDescription(url));
      onError('Store picture is not supported', 'storePicture');
    },

    createCalendarEvent: function(parameters) {
      mraidLog('debug', 'createCalendarEvent: ' + JSON.stringify(parameters));
      onError('Create calendar event is not supported', 'createCalendarEvent');
    },

    unload: function() {
      mraidLog('debug', 'unload');
      // MRAID 3.0: unload() tears the creative down. Routed to native (like close):
      // the manager dismisses on interstitial placements and rejects on inline.
      callNative('unload');
    },

    // ============================================
    // POSITION & SIZE
    // ============================================
    getCurrentPosition: function() { mraidLog('debug', 'getCurrentPosition: ' + JSON.stringify(currentPosition)); return currentPosition; },
    getDefaultPosition: function() { mraidLog('debug', 'getDefaultPosition: ' + JSON.stringify(defaultPosition)); return defaultPosition; },
    getMaxSize: function() { mraidLog('debug', 'getMaxSize: ' + JSON.stringify(maxSize)); return maxSize; },
    getScreenSize: function() { mraidLog('debug', 'getScreenSize: ' + JSON.stringify(screenSize)); return screenSize; },

    // ============================================
    // EXPAND PROPERTIES
    // ============================================
    getExpandProperties: function() {
      expandProperties.isModal = true;
      mraidLog('debug', 'getExpandProperties: ' + JSON.stringify(expandProperties));
      return expandProperties;
    },

    setExpandProperties: function(properties) {
      mraidLog('debug', 'setExpandProperties: ' + JSON.stringify(properties));
      if (!properties) return;
      if (properties.width != null && !isNaN(properties.width)) {
        expandProperties.width = properties.width;
      }
      if (properties.height != null && !isNaN(properties.height)) {
        expandProperties.height = properties.height;
      }
    },

    // ============================================
    // RESIZE PROPERTIES
    // ============================================
    getResizeProperties: function() { mraidLog('debug', 'getResizeProperties: ' + JSON.stringify(resizeProperties)); return resizeProperties; },

    setResizeProperties: function(properties) {
      mraidLog('debug', 'setResizeProperties: ' + JSON.stringify(properties));
      resizePropertiesInitialized = false;

      if (!properties) {
        onError('properties is null', 'setResizeProperties');
        return;
      }

      if (!maxSize || !maxSize.width || !maxSize.height) {
        onError('Unable to use maxSize of [' + JSON.stringify(maxSize) + ']', 'setResizeProperties');
        return;
      }

      if (properties.width == null || typeof properties.width === 'undefined' || isNaN(properties.width)) {
        onError('width param of [' + properties.width + '] is unusable.', 'setResizeProperties');
        return;
      }
      if (properties.width < 50 || properties.width > maxSize.width) {
        onError('width param of [' + properties.width + '] outside acceptable range of 50 to ' + maxSize.width, 'setResizeProperties');
        return;
      }

      if (properties.height == null || typeof properties.height === 'undefined' || isNaN(properties.height)) {
        onError('height param of [' + properties.height + '] is unusable.', 'setResizeProperties');
        return;
      }
      if (properties.height < 50 || properties.height > maxSize.height) {
        onError('height param of [' + properties.height + '] outside acceptable range of 50 to ' + maxSize.height, 'setResizeProperties');
        return;
      }

      if (properties.offsetX == null || typeof properties.offsetX === 'undefined' || isNaN(properties.offsetX)) {
        onError('offsetX param of [' + properties.offsetX + '] is unusable.', 'setResizeProperties');
        return;
      }
      if (properties.offsetY == null || typeof properties.offsetY === 'undefined' || isNaN(properties.offsetY)) {
        onError('offsetY param of [' + properties.offsetY + '] is unusable.', 'setResizeProperties');
        return;
      }

      if (typeof properties.allowOffscreen !== 'boolean') {
        onError('allowOffscreen param of [' + properties.allowOffscreen + '] is unusable.', 'setResizeProperties');
        return;
      }

      resizeProperties.width = properties.width;
      resizeProperties.height = properties.height;
      resizeProperties.customClosePosition = properties.customClosePosition || 'top-right';
      resizeProperties.offsetX = properties.offsetX;
      resizeProperties.offsetY = properties.offsetY;
      resizeProperties.allowOffscreen = properties.allowOffscreen;

      resizePropertiesInitialized = true;
    },

    // ============================================
    // ORIENTATION PROPERTIES
    // ============================================
    getOrientationProperties: function() { mraidLog('debug', 'getOrientationProperties: ' + JSON.stringify(orientationProperties)); return orientationProperties; },

    setOrientationProperties: function(properties) {
      mraidLog('debug', 'setOrientationProperties: ' + JSON.stringify(properties));
      if (properties && typeof properties === 'object') {
        if (typeof properties.allowOrientationChange === 'boolean') {
          orientationProperties.allowOrientationChange = properties.allowOrientationChange;
        }
        if (typeof properties.forceOrientation === 'string') {
          var requestedForceOrientation = properties.forceOrientation.toLowerCase();
          orientationProperties.forceOrientation =
            (requestedForceOrientation === 'portrait' ||
             requestedForceOrientation === 'landscape' ||
             requestedForceOrientation === 'none')
              ? requestedForceOrientation
              : 'none';
        }
      }
      callNative('setOrientationProperties', {
        allowOrientationChange: orientationProperties.allowOrientationChange,
        forceOrientation: orientationProperties.forceOrientation
      });
    },

    // ============================================
    // MRAID 3.0: APP ORIENTATION
    // ============================================
    getCurrentAppOrientation: function() {
      var result = { orientation: currentAppOrientation.orientation, locked: currentAppOrientation.locked };
      mraidLog('debug', 'getCurrentAppOrientation: ' + JSON.stringify(result));
      return result;
    },

    // ============================================
    // FEATURE SUPPORT
    // ============================================
    supports: function(feature) {
      var result = allSupports[feature] || false;
      mraidLog('debug', 'supports(' + feature + '): ' + result);
      return result;
    },

    // ============================================
    // LOCATION (MRAID 3.0)
    // DC-13: location is supplied via OpenRTB bid request, not MRAID.
    // ============================================
    getLocation: function() {
      mraidLog('debug', 'getLocation: unavailable (location via OpenRTB, not MRAID)');
      onError('-1', 'getLocation');
      return '-1';
    },

    // ============================================
    // MRAID 3.0: AUDIO VOLUME
    // ============================================
    getAudioVolumePercentage: function() {
      mraidLog('debug', 'getAudioVolumePercentage: ' + volumePercentage);
      return volumePercentage;
    },

    // ============================================
    // MRAID 3.0: EXPOSURE PROPERTIES
    // ============================================
    getExposureProperties: function() {
      mraidLog('debug', 'getExposureProperties: ' + JSON.stringify(lastExposure));
      return lastExposure;
    },

    // ============================================
    // USE CUSTOM CLOSE
    // ============================================
    useCustomClose: function() {
      mraidLog('warn', 'useCustomClose() is deprecated in MRAID 3.0 and ignored by this SDK');
      onError('Custom close is not supported', 'useCustomClose');
      reportUnsupportedFeature('useCustomClose');
    },

    // useCustomClose is the last public method. Native-only callbacks live on _internal below.
    _internal: {
      disposables: [],
      dispose: function() {
        var queue = this.disposables;
        this.disposables = [];
        for (var i = 0; i < queue.length; i++) {
          try { queue[i](); } catch (e) {}
        }
      },

      nativeCallComplete: function() {
        if (nativeCallQueue.length === 0) {
          nativeCallInProcess = false;
        } else {
          var message = nativeCallQueue.shift();
          postMessageToNative(message);
        }
      },

      /*
       * Native-owned entry point for the creative-facing `ready` event.
       * Native calls this after geometry is seeded, so creative ready handlers
       * observe correct position/size values instead of the screen-size placeholders.
       * Idempotent: repeated invocations are silently ignored.
       */
      markReady: function() {
        onReady();
      },

      onReady: onReady,
      onReadyExpanded: onReadyExpanded,
      onStateChange: onStateChange,
      onViewableChange: onViewableChange,
      onSizeChange: onSizeChange,
      onExposureChange: onExposureChange,
      onAudioVolumeChange: onAudioVolumeChange,
      onError: onError,

      setMaxSize: function(w, h) {
        maxSize.width = w;
        maxSize.height = h;
      },

      setCurrentAppOrientation: function(orientation, locked) {
        currentAppOrientation.orientation = orientation;
        currentAppOrientation.locked = locked;
      },

      setDefaultPosition: function(x, y, width, height) {
        defaultPosition = { x: x, y: y, width: width, height: height };
      },
      setCurrentPosition: function(x, y, width, height) {
        currentPosition = { x: x, y: y, width: width, height: height };
      },
      setScreenSize: function(width, height) {
        screenSize = { width: width, height: height };
      },
      updateSupports: function(features) {
        if (typeof features === 'string') features = JSON.parse(features);
        Object.assign(allSupports, features);
      },
    }
  };
})();

window.mraid = mraid;

// ============================================================
// Content Error Monitoring
// ============================================================
(function() {
  var reportedErrors = {};

  function safeContentErrorURLDescription(url) {
    try {
      var parsed = new URL(String(url), document.location.href);
      return parsed.host ? parsed.protocol + '//' + parsed.host : parsed.protocol.replace(':', '');
    } catch (e) {
      var value = String(url || '');
      var match = value.match(/^([a-zA-Z][a-zA-Z0-9+.-]*):/);
      return match ? match[1].toLowerCase() : '(no scheme)';
    }
  }

  function reportContentError(mediaType, src, error) {
    var key = mediaType + ':' + src;
    if (reportedErrors[key]) return;
    reportedErrors[key] = true;

    console.error('[CloudX] Content error: ' + mediaType + ' failed to load: ' + safeContentErrorURLDescription(src));
    /*
     * Fire-and-forget: bypass the native call queue so a stuck queue does not
     * suppress content error reporting. Use the same platform detection as the
     * main mraid bridge.
     */
    postMessageToNative({
      action: 'contentError',
      mediaType: mediaType,
      src: src || '(unknown)',
      error: error || 'Failed to load',
    });
  }

  function setupVideoMonitoring() {
    var videos = document.querySelectorAll('video');
    videos.forEach(function(video) {
      if (video._clxErrorMonitored) return;
      video._clxErrorMonitored = true;

      video.addEventListener('error', function(e) {
        var errorMsg = 'Unknown error';
        if (video.error) {
          switch (video.error.code) {
            case 1: errorMsg = 'MEDIA_ERR_ABORTED'; break;
            case 2: errorMsg = 'MEDIA_ERR_NETWORK'; break;
            case 3: errorMsg = 'MEDIA_ERR_DECODE'; break;
            case 4: errorMsg = 'MEDIA_ERR_SRC_NOT_SUPPORTED'; break;
          }
        }
        reportContentError('video', video.src || video.currentSrc, errorMsg);
      });

      var sources = video.querySelectorAll('source');
      sources.forEach(function(source) {
        source.addEventListener('error', function(e) {
          reportContentError('video-source', source.src, 'Source failed to load');
        });
      });
    });
  }

  function setupImageMonitoring() {
    var images = document.querySelectorAll('img');
    images.forEach(function(img) {
      if (img._clxErrorMonitored) return;
      img._clxErrorMonitored = true;

      img.addEventListener('error', function(e) {
        reportContentError('image', img.src, 'Image failed to load');
      });

      if (img.complete && img.naturalWidth === 0 && img.src) {
        reportContentError('image', img.src, 'Image failed to load (already failed)');
      }
    });
  }

  function setupMonitoring() {
    setupVideoMonitoring();
    setupImageMonitoring();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', setupMonitoring);
  } else {
    setupMonitoring();
  }

  var observer = new MutationObserver(function(mutations) {
    var shouldCheck = mutations.some(function(m) {
      return m.addedNodes.length > 0;
    });
    if (shouldCheck) {
      setupVideoMonitoring();
      setupImageMonitoring();
    }
  });
  observer.observe(document.body || document.documentElement, {
    childList: true,
    subtree: true
  });
  if (window.mraid && window.mraid._internal && window.mraid._internal.disposables) {
    window.mraid._internal.disposables.push(function() {
      observer.disconnect();
    });
  }
})();

})();
