namespace CompleteInformation.Core.Api

type IUserApi = { get: UserId -> Async<User option> }
