var EnemyManager = function (windowElement) {
    this.enemyWindowElement = windowElement;
    this.enemyListElement = windowElement.getElementsByClassName("enemyList")[0];
}

EnemyManager.prototype.enterZone = function (zone) {
};

EnemyManager.prototype.leaveZone = function (zone) {
};

EnemyManager.prototype.filtersZone = function (zone) {
    return true;
};

EnemyManager.prototype.tick = function (currentTime) {
    this.enemyListElement.innerHTML = "";

    var aggroCount = window.act.numAggros();
    for (var i = 0; i < aggroCount; ++i)
    {
        var enemy = window.act.getAggro(i);
        this.enemyListElement.innerHTML += '<div class="enemyEntry" id="enemy' + i + '">' + enemy.name + ' (' + enemy.iD + ') ' + 'HP: ' + enemy.hPPercent + '% </div>';
    }
    // Just for debugging, need to figure out how to really get per-target enmity info for the aggro list
    /*
    var enmityCount = window.act.numEnmity();
    for (var i = 0; i < enmityCount; ++i)
    {
        var enmity = window.act.getEnmity(i);
        this.enemyListElement.innerHTML += '<div class="enemyEntry" id="enemy' +  aggroCount + i + '">' + enmity.id + '</div>';
    }
    */
}

EnemyManager.prototype.processLog = function (log) {
    // FIXME: notice when a mob dies.
};

window.addEventListener("load", function () {
    window.loadCSS("enemies/enemies.css");
    
    // FIXME: This can't get loaded from a file via Javascript, because of
    // cross-origin issues.  This isn't an issue at runtime (cef can cheat
    // the sandbox with some flags), but it will make developing a pain if
    // it's required to run a proxy or pass sandbox-clobbering flags to
    // the browser.  Punt on adding more developer hurdles for now.  <_<
    var element = document.createElement("div");
    element.innerHTML =
        '<div class="enemyList">' +
        '  <div class="enemyEntry" id="enemy1">enemy1</div>' +
        '  <div class="enemyEntry" id="enemy2">enemy2</div>' +
        '  <div class="enemyEntry" id="enemy3">enemy3</div>' +
        '  <div class="enemyEntry" id="enemy4">enemy4</div>' +
        '  <div class="enemyEntry" id="enemy5">enemy5</div>' +
        '  <div class="enemyEntry" id="enemy6">enemy6</div>' +
        '  <div class="enemyEntry" id="enemy7">enemy7</div>' +
        '  <div class="enemyEntry" id="enemy8">enemy8</div>' +
        '</div>';

    var body = document.getElementsByTagName("body")[0];
    body.appendChild(element);

    var defaultGeometry = {
        width: "160px",
        height: "248px",
    };

    windowManager.add("enemies", element, "enemies", defaultGeometry);

    // FIXME: This is such a clumsy binding.
    window.enemyManager = new EnemyManager(element);
    window.updateRegistrar.register(window.enemyManager);
});