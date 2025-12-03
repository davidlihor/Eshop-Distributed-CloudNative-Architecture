import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { User } from "../../app/models/user";
import agent from '../../app/api/agent';
import { setBasket } from '../basket/basketSlice';
import keycloak from "../../app/api/keycloak";

interface AccountState {
    user: User | null
    authInitialized: Boolean
}

const initialState: AccountState = {
    user: null,
    authInitialized: false
}

export const initUserFromKeycloak = createAsyncThunk<User>('account/initUser', async () => {
    if (!keycloak.authenticated) throw new Error("Not authenticated");
    await keycloak.updateToken(30);

    const claims = JSON.parse(atob(keycloak.token!.split('.')[1]));
    const roles = claims.realm_access?.roles || [];

    return {
      username: claims.preferred_username,
      email: claims.email,
      token: keycloak.token!,
      roles
    };
});

export const fetchCurrentUser = createAsyncThunk<User>(
    'account/fetchCurrentUser',
    async (_, thunkAPI) => {
        try {
            const userDto = await agent.Account.currentUser();
            const {basket, ...user} = userDto;
            if (basket) thunkAPI.dispatch(setBasket(basket));
            localStorage.setItem('user', JSON.stringify(user));
            return user;
        } catch (error: any) {
            return thunkAPI.rejectWithValue({error: error.data});
        }
    }, 
    {
        condition: () => {
            if (!localStorage.getItem('user')) return false;
        }
    }
)

export const accountSlice = createSlice({
    name: 'account',
    initialState,
    reducers: {
        signOut: (state) => {
            state.user = null;
            keycloak.logout();
        },
        // setUser: (state, action) => {
        //     const claims = JSON.parse(atob(action.payload.token.split('.')[1]));
        //     const roles = claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
        //     state.user = {...action.payload, roles: typeof(roles) === 'string' ? [roles] : roles};
        // }
    },
    extraReducers: (builder => {
        builder.addCase(initUserFromKeycloak.fulfilled, (state, action) => {
            state.user = action.payload;
            state.authInitialized = true
            console.log(action.payload.token)
        });
        builder.addCase(initUserFromKeycloak.rejected, (state) => {
          state.user = null;
          state.authInitialized = true;
        });
        // builder.addCase(fetchCurrentUser.rejected, (state) => {
        //     state.user = null;
        //     localStorage.removeItem('user');
        //     toast.error('Session expired - please login again');
        //     router.navigate('/');
        // })
        // builder.addMatcher(isAnyOf(signInUser.fulfilled, fetchCurrentUser.fulfilled), (state, action) => {
        //     const claims = JSON.parse(atob(action.payload.token.split('.')[1]));
        //     const roles = claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
        //     state.user = {...action.payload, roles: typeof(roles) === 'string' ? [roles] : roles};
        // });
        // builder.addMatcher(isAnyOf(signInUser.rejected), (_state, action) => {
        //     throw action.payload;
        // })
    })
})

export const { signOut } = accountSlice.actions;