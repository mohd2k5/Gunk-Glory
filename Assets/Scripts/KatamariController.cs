using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class KatamariController : MonoBehaviour
{   
    private PlayerControls playerControls; //Using the Input system. If you prefer using the old one, it shouldn't be too hard to implement it into the project

    [SerializeField]
    private float speed, rotationSpeed, mouseX; //Basic katamari controls

    [SerializeField]
    private GameObject avatarObject;

    private Rigidbody rb;
    [SerializeField]
    private Camera mainCamera;  //Player camera. This project uses 3 cameras, one for the main game view, one for the player model on the corner of the screen and one that displays the last object picked by the player
    private Transform cameraLook;
    [SerializeField]
    private Transform avatarPosition; //The player model is not attached to the katamari, the model just inherits it's position

    private Vector2 mouseInput;
    [SerializeField]
    private SphereCollider katamariCollider;
    [SerializeField]
    private float KatamariSize; //Controls the size of the collision of the katamari

    private float cameraDistance = 0f; //Value used so the camera doesn't eclipses the whole screen
    
    public int maxObjCount = 20;    //Used to control the amount of objects attached to the katamari that keep their collision
                                    //Left public so the amount can be controlled by any possible optimization option
    public int objCount = 0;        //Counts the amount of object picked by the katamari. Mostly useless as of now

    //List of objects picked up by the katamari
    //This list was created with the intent of using the objects in it to add to the bumpiness of the katamari, so the items in it would retain their collision, but I'm not sure if it's that good of an idea
    private List<GameObject> pickedOjects = new List<GameObject>();

    //All of these relate to the last object grabbed by the katamari
    private GameObject lastPickedObject;
    [SerializeField]
    private Transform objCameraLoc;
    [SerializeField]
    private Camera objCamera;
    [SerializeField]
    private TextMeshProUGUI objName;

    [SerializeField]
    private GameObject primObj; //Used to avoid a few cast issues when there's not object glued to the katamari at the start of the game

    private void Awake()
    {
        playerControls = new PlayerControls();
       
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        avatarPosition = GameObject.Instantiate(avatarObject).transform;

        mainCamera = avatarPosition.GetComponent<AvatarStruct>().mainCamera;
        cameraLook = mainCamera.transform;

        rb = GetComponent<Rigidbody>();
        katamariCollider = GetComponent<SphereCollider>();

        //bounds.size should give you the actual size of the object in the world
        //Since it's a sphere Collider, any of the axis would give out the same value
        KatamariSize = katamariCollider.bounds.size.x;

        //This was added to fix an index error, I'm not sure how much sense it makes, but it did fix it
        pickedOjects.Add(primObj);
        lastPickedObject = primObj;
    }

    private void Update()
    {
        KatamariSize = katamariCollider.bounds.size.x; //Since the collider is just a sphere, any axis would do to get this value

        Vector2 move = playerControls.KatamariRoll.Move.ReadValue<Vector2>();

        //Basically, all the movement is a simple roll-a-ball script

        if (move != Vector2.zero) //Katamari only moves if there was a move input
        {
            Vector3 rollForce = new Vector3(move.x, 0, move.y);
            Vector3 camForward = cameraLook.forward;
            Vector3 camRight = cameraLook.right;

            camForward.y = 0f;
            camRight.y = 0f;

            Vector3 moveDir = rollForce.x * camRight + rollForce.z * camForward; //I can never remember this, but this piece of code makes the ball's movement relative to the camera

            rb.AddForce(moveDir * speed * Time.deltaTime);

            //Since I never played the PC version and sort of forgot to add gamepad input, the game right now just works with WASD and mouse
            //In the original game, each stick would steer the katamari into a different direction and putting both forward, would cause it to move forward
            
            //Original mechanics that could be added later:
            //Break: Pulling the sticks back would cause the katamari to stop and drift a little. Currently, it just loses momentum
            //Alternating both sticks in rapid succcession would charge the katamari and cause it to speed forward
            //Better interaction with slopes
        }

        Vector2 camRotation = playerControls.KatamariRoll.MouseLook.ReadValue<Vector2>(); //Gets the camera rotation to set the direction the character would look at
        avatarPosition.Rotate(Vector3.up, camRotation.x * Time.deltaTime * rotationSpeed);

        //This makes sure that the player character is on the floor instead of floating at the same level of the katamari
        RaycastHit hit;
        Ray groundCheck = new Ray(transform.position, Vector3.down);
        if (Physics.Raycast(groundCheck, out hit, KatamariSize))
        {
            avatarPosition.position = hit.point;
        }

        if (pickedOjects.Count >= maxObjCount) //updates the list of the last objects picked. Useless as of now
        {
            pickedOjects.RemoveAt(maxObjCount - 1);
        }


    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<KatamariStick>()) //Checks if the object has the script necessary. In hindsight, the script wasn't exactly needed, it's just here for the same of organization
                                                                //Although, if you want the name of the object to show up properly instead of how the asset shows up in the Hierarchy (like "bottle10(clone)" for instance), it's best to do so using the script
        {
            float objColSize = collision.gameObject.GetComponent<KatamariStick>().size;
            if (objColSize < KatamariSize) //As long as the object is smaller, it shall be glued to the katamari. The series could've played around with heavy objects vs light ones, who knows...
            {
                collision.gameObject.transform.parent = gameObject.transform; //Parents the object picked to the katamari
                collision.gameObject.GetComponent<Rigidbody>().isKinematic = true;
                collision.gameObject.GetComponent<Rigidbody>().useGravity = false;
                collision.gameObject.GetComponent<Rigidbody>().detectCollisions = false;

                pickedOjects.Add(collision.gameObject); //that list again
                katamariCollider.radius += objColSize / 50;    //Adds part of the picked object to the size of the collider of the katamari
                                                               //I couldn't find a good ratio for this, even at very small amounts
                                                               //Too much causes the collider to grow a lot and some objects end up hovering above the surface, but too sm
                                                               //A amount makes it so the katamari can't pick up larger objects when it looks like it should

                cameraDistance += objColSize / 50;

                AttachCamera(collision.gameObject, objColSize); //attaches the camera that shows the object on the corner of the screen

                objName.text = collision.gameObject.GetComponent<KatamariStick>().objName; //Displays the name of the object on the left corner

                objCount += 1;
            }
        }
    }

    private void AttachCamera (GameObject objToAttach, float objSize)
    {
        //The camera that displays the object uses a layer mask to isolate that object, so the object is moved into that layer until the katamari picks up another object
        lastPickedObject.layer = LayerMask.NameToLayer("Default"); //Puts the last object back to the default layer

        objToAttach.layer = LayerMask.NameToLayer("LastPickedObject"); //Moves the object to the "LastPickedObject" layer

        objCameraLoc.parent = objToAttach.transform; //Parents the camera to the object
        objCamera.transform.localPosition = new Vector3(objCamera.transform.localPosition.x, objCamera.transform.localPosition.y, objSize * 2); //Tries to set the camera at a sensible distance to the object
        objCamera.transform.localRotation = objToAttach.transform.rotation; //Tries to set the rotation of the object camera to the object... tries

        objCamera.transform.LookAt(objToAttach.transform.position); //Points the camera to the object just to be sure

        lastPickedObject = objToAttach; //sets the object to a variable so it can be removed from the local camera layer when the next object is picked up
    }


    //I'm so glad commenting doesn't have grammar correction
}
