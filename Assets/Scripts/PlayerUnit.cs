using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUnit : MonoBehaviour
{
    public string id;
    
    public bool isLocalPlayer;
    public GameObject cameraSpot;
    Material material;

    public float moveSpeed = 1.0f;
    public float angularSpeed = 60.0f;

    public Transform revTransform;
    public Transform delayedTransform;

    /// <summary>
    /// projectile variables
    /// </summary>
    public Transform bulletSpawnerTransform;
    public Bullet bulletPrefab;
    public float weaponCooldown = 0.5f;
    public float cooldownTime;

    /// <summary>
    /// player properties
    /// </summary>
    public float currentHealth;
    public float maxHealth = 100;
    public bool IsAlive { get { return currentHealth > 0; } }

    /// <summary>
    /// UI features
    /// </summary>
    public TextMeshProUGUI clientIdText;
    public Slider healthBar;

    public void Awake()
    {
        var cubeRenderer = GetComponentInChildren<Renderer>();
        material = cubeRenderer.material;
    }

    private void Start()
    {
        currentHealth = maxHealth;
        healthBar.value = 1.0f;
    }

    public void FixedUpdate()
    {
        if( isLocalPlayer )
        {
            Transform curTransform = transform;
            if( Input.GetKey( KeyCode.W ) )
            {
                curTransform.position += curTransform.forward * Time.deltaTime * moveSpeed;
            }
            if( Input.GetKey( KeyCode.S ) )
            {
                curTransform.position -= curTransform.forward * Time.deltaTime * moveSpeed;
            }
            if( Input.GetKey( KeyCode.A ) )
            {
                curTransform.position -= curTransform.right * Time.deltaTime * moveSpeed;
            }
            if( Input.GetKey( KeyCode.D ) )
            {
                curTransform.position += curTransform.right * Time.deltaTime * moveSpeed;
            }
            if( Input.GetKeyUp( KeyCode.Space ))
            {
                NetworkMan.Instance.SendAction("fire", bulletSpawnerTransform );
                //FireBullet();
            }

            // mouse right drag
            if( Input.GetMouseButton( 1 ) )
            {
                float rotation = Input.GetAxis( "Mouse X" ) * angularSpeed;
                curTransform.Rotate( Vector3.up, rotation );
            }

            if( CanvasManager.Instance.prediction.isOn )
            {
                StartCoroutine( UpdateTransform( curTransform, NetworkMan.Instance.estimatedLag ) );
            }
            else
            {
                transform.position = curTransform.position;
                transform.rotation = curTransform.rotation;
            }
        }
        else
        {
            clientIdText.transform.rotation = Camera.main.transform.rotation;
        }

        if( cooldownTime > 0.0f )
        {
            cooldownTime -= Time.fixedDeltaTime;
        }
    }

    public void SetId( string clientId, bool isLocal )
    {
        id = clientId;
        if( clientIdText != null )
        {
            clientIdText.text = id.Split( new char[] { '(', ',', ')' } )[2];
            clientIdText.color = isLocal ? Color.red : Color.gray;
        }

        isLocalPlayer = isLocal;
        if( isLocal )
        {
            Camera.main.transform.parent = cameraSpot.transform;
            Camera.main.transform.localPosition = Vector3.zero;
            Camera.main.transform.localRotation = Quaternion.identity;
        }
    }

    IEnumerator UpdateTransform( Transform newTransform, float waittingTime )
    {
        yield return new WaitForSeconds( waittingTime );
        transform.position = newTransform.position;
        transform.rotation = newTransform.rotation;
    }

    public void SetColor( Color color )
    {
        material.SetColor( "_Color", color );
    }

    public void FireBullet()
    {
        if( cooldownTime <= 0.0f )
        {
            Bullet bullet = Instantiate( bulletPrefab, bulletSpawnerTransform.position, bulletSpawnerTransform.rotation );
            bullet.ownerId = id;
            bullet.Fire();
            cooldownTime = weaponCooldown; 
        }
        //NetworkMan.Instance.SendAction("fire", bulletSpawnerTransform );
    }

    public void TakeDamage( float damage )
    {
        currentHealth = Mathf.Max( currentHealth - damage, 0.0f );
        SetHealth(currentHealth);
    }

    public void SetHealth( float health )
    {
        currentHealth = health;
        healthBar.value = currentHealth / maxHealth;
        if (currentHealth <= 0)
        {
            //StartCoroutine(Die());
            Debug.Log("Player Die");
        }
    }

    IEnumerator Die()
    {
        gameObject.SetActive(false);
        yield return new WaitForSeconds(1.0f);
        //Destroy(gameObject);
    }
}
