using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerTransform : NetworkTransform
{
    private NetworkVariable<Vector3> netPos = new(readPerm:NetworkVariableReadPermission.Everyone,writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<Quaternion> netRot = new(readPerm: NetworkVariableReadPermission.Everyone,writePerm: NetworkVariableWritePermission.Owner);

    protected override void Update()
    {
        if (IsOwner)
        {
            netPos.Value = transform.position;
            netRot.Value = transform.rotation;
        }
        else
        {
            transform.position = netPos.Value;
            transform.rotation = netRot.Value;
        }
    }
}
