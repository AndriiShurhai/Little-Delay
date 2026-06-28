using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


public interface ICharacterPhysics
{
    Transform Transform { get; }
    void ApplyDeflectionForce(Vector3 force);
}