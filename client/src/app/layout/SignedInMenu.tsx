import { Button, Menu, Fade, MenuItem } from "@mui/material";
import { useState } from "react";
import { signOut } from "../../features/account/accountSlice";
import { useAppDispatch, useAppSelector } from "../store/configureStore";
import { clearBasket } from '../../features/basket/basketSlice';
import { Link } from 'react-router-dom';
import { ListAltOutlined, LogoutRounded, Person2Outlined } from "@mui/icons-material";

export default function SignedInMenu() {
  const dispatch = useAppDispatch();
  const { user } = useAppSelector(state => state.account);
  const [anchorEl, setAnchorEl] = useState(null);
  const open = Boolean(anchorEl);

  const handleClick = (event: any) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  return (
    <>
      <Button
        color='inherit'
        onClick={handleClick}
        sx={{ typography: 'h6' }}
      >
        
        {user?.username}
      </Button>
      <Menu
        anchorEl={anchorEl}
        open={open}
        onClose={handleClose}
        TransitionComponent={Fade}
        style={{"minWidth": "100px"}}
      >
        <MenuItem onClick={handleClose}><Person2Outlined />Profile</MenuItem>
        <MenuItem component={Link} to='/orders'>
        <ListAltOutlined/>
        Orders
        </MenuItem>
        <MenuItem 
        onClick={() => {
          dispatch(signOut());
          dispatch(clearBasket());
        }}>
          <LogoutRounded />
          Logout
        </MenuItem>
      </Menu>
    </>
  );
}