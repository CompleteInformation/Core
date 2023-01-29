namespace CompleteInformation.Core

// TODO: Change to UUID
type UserId = UserId of int

module UserId =
    let unwrap (UserId id) = id

type User = { id: UserId; name: string }

module User =
    let create id name = { id = id; name = name }
