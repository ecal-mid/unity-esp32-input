const INPUT_SERVICE_UUID = "78880000-d9af-4592-a7ac-4d5830390106"
const INPUT_SERVICE_ENCODER_CHARACTERISTIC_UUID = "78880001-d9af-4592-a7ac-4d5830390106"
const INPUT_SERVICE_BUTTON_CHARACTERISTIC_UUID = "78880002-d9af-4592-a7ac-4d5830390106"


let server;


async function observeCharacteristic<T>(options:{
    service: BluetoothRemoteGATTService,
    characteristicUUID:string,
    decode:(data:DataView)=>T,
    onValueChanged:(value:T)=>void
}){

    // wait for updates
    const characteristic = await options.service.getCharacteristic(options.characteristicUUID)
    characteristic.addEventListener('characteristicvaluechanged', (event) => {

        const data = (event.target as BluetoothRemoteGATTCharacteristic).value
        if(data.byteLength > 0)
        {
            const value = options.decode(data)
            options.onValueChanged(value);
        }
    });
    await characteristic.startNotifications();

    // read initial value
    const data = await characteristic.readValue()
    const value = options.decode(data)
    options.onValueChanged(value);
}

async function connect() {

    const device = await navigator.bluetooth.requestDevice({

        filters: [
            {
                services: [
                    //'battery_service',
                    INPUT_SERVICE_UUID
                ]
            }]
    })


    device.addEventListener('gattserverdisconnected', disconnected);

    server = await device.gatt.connect()

    connected()

    const service = await server.getPrimaryService(INPUT_SERVICE_UUID)

    await observeCharacteristic<number>({
        service:service,
        characteristicUUID:INPUT_SERVICE_ENCODER_CHARACTERISTIC_UUID,
        decode:(data: DataView) => data.getInt32(0, true),
        onValueChanged:(value)=> updateEncoderValue(value)
    })

    await observeCharacteristic<number>({
        service:service,
        characteristicUUID:INPUT_SERVICE_BUTTON_CHARACTERISTIC_UUID,
        decode:(data: DataView) => data.getInt32(0, true),
        onValueChanged:(value)=> updateButtonValue(value == 1)
    })
}

async function disconnect(){

    if(server)
        server.disconnect();
}


const connectButton = document.querySelector<HTMLButtonElement>("button[data-action=connect]")
const disconnectButton = document.querySelector<HTMLButtonElement>("button[data-action=disconnect]")

connectButton.addEventListener("click", () => connect())
disconnectButton.addEventListener("click", () => disconnect())

function connected(){

    console.log("connected")

    showInfo(true)
    updateButtons()
}
function disconnected(){

    console.log("disconnected")
    showInfo(false)
    updateButtons()
}

function updateEncoderValue(value:number){

    document.querySelector("span[data-value=encoder]").innerHTML = value.toString()
}

function updateButtonValue(value:boolean){

    document.querySelector("span[data-value=button]").innerHTML = (value ? 1 : 0).toString()
}

function updateButtons(){
    connectButton.disabled = server !== undefined
    disconnectButton.disabled = server === undefined
}

function showInfo(on){
    document.querySelector<HTMLElement>("div#input_service").style.display = on?"block":"none"
}

showInfo(false)
updateButtons()