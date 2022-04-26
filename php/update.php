<?php
// load file
$data = file_get_contents('devices.json');

// decode json to associative array
$json_data = json_decode($data, true)["data"];

$NAME = $_GET['name'];
$IP = $_GET['ip'];
$WIFI = $_GET['wifi'];
$BATTERY = $_GET['battery'];
$MOTOR = $_GET['motor'];
$FIRMWARE = $_GET['firmware'];

if ($NAME != "controller0") { // avoid saving default name

    // check if this device is already registered
    $exists = false;
    foreach ($json_data as $key => $value) {
        if ($value['name'] == $NAME) {
            // Update
            $json_data[$key]['ip'] = $IP;
            $json_data[$key]['wifi'] = $WIFI;
            $json_data[$key]['battery'] = $BATTERY;
            $json_data[$key]['motor'] = $MOTOR;
            $json_data[$key]['firmware'] = $FIRMWARE;
            $json_data[$key]['lastupdate'] = date("d.m.Y H:i:s");
            $exists = true;
            break;
        }
    }

    if ($exists == false) {
        // Add new Device
        array_push($json_data, array('name' => $NAME, 'ip' => $IP, 'wifi' => $WIFI, 'battery' => $BATTERY, 'motor' => $MOTOR, 'firmware' => $FIRMWARE, 'lastupdate' => date("d.m.Y H:i:s")));
    }

    // encode json and save to file
    file_put_contents('devices.json', json_encode(array("data" => $json_data)));
}
// send response
echo "OK IP PUBLISHED";
