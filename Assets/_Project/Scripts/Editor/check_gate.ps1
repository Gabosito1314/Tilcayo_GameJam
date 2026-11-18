foreach ($scene in @("Scene_Level_01.unity", "Scene_Level_02.unity", "Scene_Level_03.unity")) {
    $path = "Assets\Scenes\" + $scene
    $text = [System.IO.File]::ReadAllText($path)
    
    Write-Output "=== $scene ==="
    # Find Level_Gate
    if ($text -match 'Level_Gate[\s\S]*?m_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}') {
        Write-Output "Level_Gate: ($($matches[1]), $($matches[2]))"
    } else {
        # Check prefab instance modifications for gate
        $m = [regex]::Matches($text, 'propertyPath: m_LocalPosition\.x\r?\n\s+value: ([^\r\n]+)')
        Write-Output "Found $($m.Count) position x modifications in prefabs"
    }
}
