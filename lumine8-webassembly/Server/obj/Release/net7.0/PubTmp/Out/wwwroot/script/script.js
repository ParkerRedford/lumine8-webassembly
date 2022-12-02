function getHeight() {
    let f = document.getElementById("feed");
    let h = document.getElementById("header");
    let n = document.getElementById("nav");

    if (window.matchMedia("(min-width: 769px)" && f != null).matches) {
        f.style.height = (window.innerHeight - f.offsetTop) + "px";

        if (n === null) {
            f.style.top = h.clientHeight + "px";
        }
    }
}

function setChatSize(el) {
    let h = (document.documentElement.clientHeight / Number(2));
    el.style.height = h + "px";
}

function setChatWidthInitial(ch, fr) {
    ch.style.right = (fr.clientWidth + 16) + "px";
}

function setChatWidth(ch, fr) {
    ch.style.right = fr.clientWidth + "px";
}

function setAreaHeight(a) {
    a.style.height = "auto";
    a.style.height = a.scrollHeight + "px";
}

function setScroll(el) {
    el.scrollTop = el.scrollHeight;
}

function setFriendsHeight(div, nav) {
    div.style.height = document.documentElement.clientHeight - nav.clientHeight + "px";
    div.style.maxHeight = document.documentElement.clientHeight - nav.clientHeight + "px";
}

function CheckMobile() {
    DotNet.invokeMethodAsync("lumine8-webassembly.Client", "IsMobile", window.matchMedia("(max-width: 920px)").matches);
}

function setDivHeight() {
    let i = document.getElementById("info");
    let d = document.getElementById("divPad")

    d.style.height = i.clientHeight + "px";
}

function ScrollInitial() {
    let f = document.getElementById("afterFeed");
    f.style.overflow = "auto";
    f.style.height = (window.innerHeight - f.offsetTop) + "px";
}

function generateQRCode(text) {
    let img = document.getElementById("img");
    img.src = "https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=" + text;
}

function updateQRCode(text) {
    var element = document.getElementById("qrcode");

    var bodyElement = document.body;
    if (element.lastChild)
        element.replaceChild(showQRCode(text), element.lastChild);
    else
        element.appendChild(showQRCode(text));
}

function scroll() {
    let f = document.getElementById("feed");
    let s = document.getElementById("settings");
    let h = document.getElementById("header").style.height;

    let d = window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight;

    f.style.height = d;
    s.style.height = d;
}

function media() {
    navigator.mediaDevices.getDisplayMedia({ video: true })
        .then(stream => {
            console.log("Get stream", stream);

            let v = document.getElementById("video");
            const iceConfig = {
                iceServer: [
                    {
                        urls: '172.18.96.1:5349',
                        username: 'parker',
                        credentials: '69d679e8f6ec6435998b9073a29e365dcfb862f88e1039ab39e337396cf1249d'
                    }
                ]
            }

            const peerConnection = new RTCPeerConnection(iceConfig);
            stream.getTracks().forEach(track => {
                peerConnection.addTrack(track, stream);
            });

            const remoteStream = new MediaStream();
            const remoteVideo = document.querySelector("#video");
            remoteVideo.srcObject = remoteStream;

            peerConnection.addEventListener("track", async (event) => {
                remoteStream.addTrack(event.track, remoteStream);
            });
        })
        .catch(error => {
            console.error("Error accessing media devices", error);
        });
}

function profileScroll() {
    let f = document.getElementById("feed");
    let s = document.getElementById("nav");
    let h = document.getElementById("header");

    alert(s.clientHeight);

    f.style.height = (document.documentElement.clientHeight - (h.clientHeight + 16 + s.clientHeight + 16)) + "px";
}

function windowLoad() {
    getHeight();
}

window.addEventListener("resize", function () {
    getHeight();

    ScrollInitial();
});

function ScrollDown() {
    let m = document.getElementById("messages");
    //m.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'nearest' });
    m.scrollTop = m.scrollHeight;
}

function playMessage() {
    let a = document.getElementById("audio");
    a.volume = 0.3;
    a.play();
}

function ChangeTitle(title) {
    document.title = title;
}

function _base64ToArrayBuffer(base64) {
    var binary_string = window.atob(base64);
    var len = binary_string.length;
    var bytes = new Uint8Array(len);

    for (var i = 0; i < len; i++) {
        bytes[i] = binary_string.charCodeAt(i);
    }

    return bytes.buffer;
}

function loadVideo3(base64, contentType, video) {
    var arrayBuffer = _base64ToArrayBuffer(base64);

    const blob = new Blob([arrayBuffer], { type: contentType });
    var dataUrl = window.URL.createObjectURL(blob);

    video.src = dataUrl;
}

/*function setVWidth(list, videos) {
    let vs = document.getElementsByClassName("grid-item");
    vs.forEach(el => el.style.width = ((document.documentElement.clientWidth - el.clientWidth) / 25) + "px");

    //videos.style.width = (document.documentElement.clientWidth - list.clientWidth) + "px";
}*/
