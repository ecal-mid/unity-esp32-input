<!DOCTYPE html>
<html lang="en">

<head>
    <meta charset="UTF-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Magic Leap - Devices IP Table</title>
    <style>
        body {
            background-color: lightgrey;
            color: black;
            font-family: 'Courier New', Courier, monospace;
        }

        table {
            width: 800px;
        }

        table th {
            text-align: left;
        }
    </style>
</head>

<body>

    <?php
    // load file
    $data = file_get_contents('devices.json');

    // decode json to associative array
    $json_data = json_decode($data, true)["data"];
    //print_r($json_data);
    $html = '<table>
  <tr>
    <th>Name</th>
    <th>IP</th> 
    <th>Wifi</th>
     <th>Battery</th>
     <th>Motor</th>
    <th>Firmware</th>
    <th>lastupdate</th>
  </tr>';

    foreach ($json_data as $key => $value) {
        $html .= "<tr>";
        $html .=  "<td>" . $value['name'] . "</td>";
        $html .=  "<td>" . $value['ip'] . "</td>";
        $html .=  "<td>" . $value['wifi'] . "</td>";
        $html .=  "<td>" . $value['battery'] . "V" . "</td>";
        $html .=  "<td>" . $value['motor'] . "</td>";
        $html .=  "<td>" . $value['firmware'] . "</td>";
        $html .=  "<td>" . $value['lastupdate'] . "</td>";
        $html .= "</tr>";
    }
    $html .= "</table>";

    echo $html;

    ?>

</body>

</html>