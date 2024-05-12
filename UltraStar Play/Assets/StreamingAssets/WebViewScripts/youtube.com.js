function getPlayer() {
    return document.getElementById('movie_player');
}

setInterval(() => checkPlayerState(), 1000);
postMessage({ type: 'CanLoadUrl', value: true });

var wasReady = false;
var lastPlayerState = 0;
function checkPlayerState() {
    const currentPlayerState = getPlayer().getPlayerState();
    if (lastPlayerState !== currentPlayerState) {
        lastPlayerState = currentPlayerState;
        onPlayerStateChange(currentPlayerState);
    }
}

function onPlayerStateChange(newPlayerState) {
    // console.log("onPlayerStateChange: " + newPlayerState);

    if (getPlayer().getVideoLoadedFraction() > 0 && !wasReady) {
        wasReady = true;
        postMessage({type: 'Ready', value: true});

        const durationInMillis = getPlayer().getDuration() * 1000;
        postMessage({type: 'DurationInMillis', value: durationInMillis});

        // newPlayerState is
        // -1 => not started
        //  0 => finished
        //  1 => playing
        //  2 => paused
        //  3 => buffering
        //  5 => video positioned
        if (newPlayerState.data === 1) {
            postMessage({type: 'StartedOrResumed', value: true});
        } else {
            postMessage({type: 'StoppedOrPaused', value: true});
        }
    }
}

function loadUrl(url)
{
    if (url.startsWith("{{")) {
        console.log("url is not set. Using fallback value instead.");
        url = fallbackUrl;
    }

    if (url.startsWith("{{")) {
        console.log("url is not set even after using fallback value. Aborting.");
        return;
    }

    const urlObject = new URL(url);
    const videoId = urlObject.searchParams.get('v');

    getPlayer().loadVideoById(videoId);
}

function sendPlaybackPositionInMillis() {
    const result = getPlayer().getCurrentTime() * 1000;
    // console.log("sendPlaybackPositionInMillis: " + result);
    postMessage({type: 'PlaybackPositionInMillis', value: result});
    return result;
}

function setPlaybackPositionInMillis(positionInMillis) {
    // console.log("setPlaybackPositionInMillis: " + positionInMillis);
    getPlayer().seekTo(positionInMillis / 1000, true);
}

function setVolume(volumeZeroToHundred) {
    // console.log("setVolume: " + volumeZeroToHundred);
    getPlayer().setVolume(volumeZeroToHundred);
}

function resumePlayback() {
    // console.log("resumePlayback");
    postMessage({type: 'StartedOrResumed', value: true});
    return getPlayer().playVideo();
}

function pausePlayback() {
    // console.log("pausePlayback");
    postMessage({type: 'StoppedOrPaused', value: true});
    return getPlayer().pauseVideo();
}

function stopPlayback() {
    // console.log("stopPlayback");
    postMessage({type: 'StoppedOrPaused', value: true});
    return getPlayer().stopVideo();
}

function postMessage(jsonObject) {
    if (typeof vuplex === 'undefined' || !vuplex || !vuplex.postMessage) {
        // console.log("postMessage: vuplex is not defined, ignoring message: " + JSON.stringify(jsonObject));
        return;
    }
    vuplex.postMessage(jsonObject);
}