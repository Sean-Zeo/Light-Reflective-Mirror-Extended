using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class PlayerSample : NetworkBehaviour
{
    public CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    public float playerSpeed = 2.0f;
    public float jumpHeight = 1.0f;
    private float gravityValue = -9.81f;
    public LightReflectiveMirror.PlayerAccountInfo accountInfo;
    public TextMesh playerNameTextMesh;

    [SyncVar]
    public Vector3 syncPosition;
    // Update is called once per frame
    void Update()
    {
        if (!isLocalPlayer) {
            transform.position = Vector3.Lerp(transform.position, syncPosition, 8*Time.deltaTime);
            return; 
        }

        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        controller.Move(move * Time.deltaTime * playerSpeed);

        if (move != Vector3.zero)
        {
            gameObject.transform.forward = move;
        }

        // Changes the height position of the player..
        if (Input.GetButtonDown("Jump") && groundedPlayer)
        {
            playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        SendSyncPosition(transform.position);
    }

    [Command]
    void SendSyncPosition(Vector3 newPos)
    {
        syncPosition = newPos;
    }

    private void LateUpdate()
    {
        playerNameTextMesh.text = accountInfo.playerName;
        playerNameTextMesh.transform.LookAt(Camera.main.transform);
    }
}
