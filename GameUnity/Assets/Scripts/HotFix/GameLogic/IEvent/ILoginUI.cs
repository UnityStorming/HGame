using System.Collections;
using System.Collections.Generic;
using HGame;
using UnityEngine;

namespace GameLogic
{
    [EventInterface(EEventGroup.GroupUI)]
    public interface ILoginUI
    {
        void ShowLoginUI();
    }
}