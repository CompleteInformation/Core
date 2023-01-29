namespace CompleteInformation.Core

open System

type UserId = UserId of Guid

module UserId =
    let toString (UserId id) = id.ToString()

type User = { id: UserId; name: string }

module User =
    let create id name = { id = id; name = name }
