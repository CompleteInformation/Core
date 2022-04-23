namespace CompleteInformation.Core.Api

type IUserApi =
    { geti: Async<User option>
      get: UserId -> Async<User option> }
