using Unity.VisualScripting;
using UnityEngine;

public class unpowerWireBehave : MonoBehaviour
{
    unpoweredWireStats unpoweredWireS;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       unpoweredWireS = gameObject.GetComponent<unpoweredWireStats>();
    }

    // Update is called once per frame
    void Update()
    {
        if (unpoweredWireS.connected)
        {
            unpoweredWireS.poweredLight.SetActive(true);
            unpoweredWireS.unpoweredLight.SetActive(false);
        }
        else
        {
            unpoweredWireS.poweredLight.SetActive(false);
            unpoweredWireS.unpoweredLight.SetActive(true);
        }
    }
        void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<PoweredWireStats>())
        {
            PoweredWireStats powerWireS = collision.GetComponent<PoweredWireStats>();
            if(powerWireS.wireColor == unpoweredWireS.objectColor)
            {
                powerWireS.connected = true;
                unpoweredWireS.connected = true;
                powerWireS.connectedPosition = gameObject.transform.position;
            }

        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<PoweredWireStats>())
        {
            PoweredWireStats powerWireS = collision.GetComponent<PoweredWireStats>();
            powerWireS.connected = false;
            unpoweredWireS.connected = false;
      
        }
    }
 
}
